<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { projectsApi } from '@/api/projects'
import { documentsApi } from '@/api/documents'
import { templatesApi } from '@/api/templates'
import { categoriesApi } from '@/api/categories'
import { ApiError } from '@/api/httpClient'
import type {
  CategoryDto,
  ProjectDocumentSummaryDto,
  ProjectDto,
  ProjectMemberDto,
  ProjectRole,
  TemplateSummaryDto,
} from '@/api/types'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const projectId = route.params.id as string

const project = ref<ProjectDto | null>(null)
const members = ref<ProjectMemberDto[]>([])
const documents = ref<ProjectDocumentSummaryDto[]>([])
const loading = ref(true)
const loadError = ref<string | null>(null)

// The backend has no concept of "my own user id" the SPA can read
// directly from the token (that mapping only exists server-side via
// IUserProvisioningService) — matching by email, which is already in
// both the ID token and every ProjectMemberDto, is the pragmatic way to
// find "me" in the member list. See GetCurrentUserQueryHandler's own doc
// comment for the same reasoning from the other direction.
const myEmail = computed(() => authStore.user?.profile.email)
const myMembership = computed(() => members.value.find((m) => m.userEmail === myEmail.value))
const isOwner = computed(() => myMembership.value?.role === 'Owner')
// Mirrors CreateProjectDocumentCommandHandler/CreateDocumentRevisionCommandHandler's
// own check: Viewer may read, Editor or Owner may create.
const canEditDocuments = computed(
  () => myMembership.value?.role === 'Editor' || myMembership.value?.role === 'Owner',
)

const roleOptions: ProjectRole[] = ['Viewer', 'Editor', 'Owner']

const addUserId = ref('')
const addRole = ref<ProjectRole>('Viewer')
const addBusy = ref(false)
const addError = ref<string | null>(null)

const rowBusy = ref<string | null>(null)
const rowError = ref<string | null>(null)

async function load() {
  loading.value = true
  loadError.value = null
  try {
    const [projectResult, membersResult, documentsResult] = await Promise.all([
      projectsApi.get(projectId),
      projectsApi.listMembers(projectId),
      documentsApi.listForProject(projectId),
    ])
    project.value = projectResult
    members.value = membersResult
    documents.value = documentsResult
  } catch (err) {
    loadError.value = err instanceof Error ? err.message : 'Failed to load project.'
  } finally {
    loading.value = false
  }
}

// Categories and templates are only needed once someone who can actually
// create a document opens the dialog — loaded lazily rather than on every
// project view, since most visits (Viewers, or Editors just checking
// members) never touch this.
const categories = ref<CategoryDto[]>([])
const templates = ref<TemplateSummaryDto[]>([])
const createOptionsLoaded = ref(false)

const createDialogOpen = ref(false)
const newCategoryId = ref('')
const newTitle = ref('')
const newSourceTemplateId = ref<string | null>(null)
const newContent = ref('')
const creatingDocument = ref(false)
const createDocumentError = ref<string | null>(null)

async function openCreateDialog() {
  createDocumentError.value = null
  if (!createOptionsLoaded.value) {
    try {
      const [categoriesResult, templatesResult] = await Promise.all([
        categoriesApi.list(),
        templatesApi.list(),
      ])
      categories.value = categoriesResult
      templates.value = templatesResult
      createOptionsLoaded.value = true
    } catch (err) {
      createDocumentError.value =
        err instanceof Error ? err.message : 'Failed to load categories and templates.'
    }
  }
  createDialogOpen.value = true
}

// Prefills category + content from the chosen template, but leaves both
// editable afterward — picking a starting point, not locking the user
// into it.
async function onTemplateSelected(templateId: string | null) {
  newSourceTemplateId.value = templateId
  if (!templateId) {
    return
  }
  try {
    const template = await templatesApi.get(templateId)
    newContent.value = template.content
    if (!newCategoryId.value) {
      newCategoryId.value = template.categoryId
    }
  } catch (err) {
    createDocumentError.value = err instanceof Error ? err.message : 'Failed to load template content.'
  }
}

async function createDocument() {
  if (!newCategoryId.value || !newTitle.value.trim()) {
    return
  }
  creatingDocument.value = true
  createDocumentError.value = null
  try {
    // A source template's own currentRevisionId, not its Id — matches
    // CreateProjectDocumentCommand's SourceTemplateRevisionId, which
    // points at the specific revision the document's content came from.
    let sourceTemplateRevisionId: string | null = null
    if (newSourceTemplateId.value) {
      const template = await templatesApi.get(newSourceTemplateId.value)
      sourceTemplateRevisionId = template.currentRevisionId
    }
    const created = await documentsApi.create(
      projectId,
      newCategoryId.value,
      newTitle.value.trim(),
      newContent.value,
      sourceTemplateRevisionId,
    )
    createDialogOpen.value = false
    newCategoryId.value = ''
    newTitle.value = ''
    newSourceTemplateId.value = null
    newContent.value = ''
    await router.push(`/documents/${created.id}`)
  } catch (err) {
    createDocumentError.value = apiErrorMessage(err, 'Failed to create document.')
  } finally {
    creatingDocument.value = false
  }
}

function apiErrorMessage(err: unknown, fallback: string): string {
  return err instanceof ApiError ? ((err.body as { error?: string })?.error ?? fallback) : fallback
}

async function addMember() {
  if (!addUserId.value.trim()) {
    return
  }
  addBusy.value = true
  addError.value = null
  try {
    const added = await projectsApi.addMember(projectId, addUserId.value.trim(), addRole.value)
    members.value.push(added)
    addUserId.value = ''
    addRole.value = 'Viewer'
  } catch (err) {
    addError.value = apiErrorMessage(err, 'Failed to add member.')
  } finally {
    addBusy.value = false
  }
}

async function changeRole(member: ProjectMemberDto, newRole: ProjectRole) {
  rowBusy.value = member.userId
  rowError.value = null
  const previousRole = member.role
  try {
    const updated = await projectsApi.updateMemberRole(projectId, member.userId, newRole)
    member.role = updated.role
  } catch (err) {
    member.role = previousRole
    rowError.value = apiErrorMessage(err, 'Failed to update role.')
  } finally {
    rowBusy.value = null
  }
}

async function removeMember(member: ProjectMemberDto) {
  rowBusy.value = member.userId
  rowError.value = null
  try {
    await projectsApi.removeMember(projectId, member.userId)
    members.value = members.value.filter((m) => m.userId !== member.userId)
  } catch (err) {
    rowError.value = apiErrorMessage(err, 'Failed to remove member.')
  } finally {
    rowBusy.value = null
  }
}

onMounted(load)
</script>

<template>
  <v-container>
    <v-progress-linear v-if="loading" indeterminate class="mb-4" />
    <v-alert v-if="loadError" type="error" :text="loadError" />

    <template v-if="!loading && !loadError && project">
      <h1 class="text-h5 mb-4">{{ project.name }}</h1>

      <v-card title="Documents" class="mb-4">
        <v-btn
          v-if="canEditDocuments"
          id="new-document-button"
          color="primary"
          prepend-icon="mdi-plus"
          class="ma-4 mb-0"
          @click="openCreateDialog"
        >
          New Document
        </v-btn>

        <v-card-text v-if="documents.length === 0">
          <span class="text-medium-emphasis">No documents yet.</span>
        </v-card-text>
        <v-list v-else lines="one">
          <v-list-item
            v-for="doc in documents"
            :key="doc.id"
            :title="doc.title"
            :subtitle="`v${doc.currentVersion}`"
            prepend-icon="mdi-file-document-edit-outline"
            @click="router.push(`/documents/${doc.id}`)"
          />
        </v-list>
      </v-card>

      <v-card title="Members">
        <v-alert v-if="rowError" type="error" :text="rowError" class="ma-4" />

        <v-table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th v-if="isOwner" class="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="member in members" :key="member.userId">
              <td>{{ member.userName }}</td>
              <td>{{ member.userEmail }}</td>
              <td>
                <v-select
                  v-if="isOwner"
                  :model-value="member.role"
                  :items="roleOptions"
                  density="compact"
                  hide-details
                  variant="underlined"
                  style="max-width: 140px"
                  :disabled="rowBusy === member.userId"
                  @update:model-value="(value: unknown) => changeRole(member, value as ProjectRole)"
                />
                <span v-else>{{ member.role }}</span>
              </td>
              <td v-if="isOwner" class="text-right">
                <v-btn
                  icon="mdi-delete-outline"
                  variant="text"
                  size="small"
                  :loading="rowBusy === member.userId"
                  @click="removeMember(member)"
                />
              </td>
            </tr>
          </tbody>
        </v-table>

        <v-card-text v-if="isOwner">
          <v-divider class="mb-4" />
          <v-alert v-if="addError" type="error" :text="addError" class="mb-4" />
          <div class="d-flex align-center ga-2">
            <v-text-field
              id="add-member-user-id"
              v-model="addUserId"
              label="User ID"
              hint="Ask the person to share their User ID from their account page"
              persistent-hint
              density="compact"
              hide-details="auto"
            />
            <v-select
              v-model="addRole"
              :items="roleOptions"
              label="Role"
              density="compact"
              hide-details
              style="max-width: 140px"
            />
            <v-btn id="confirm-add-member" color="primary" :loading="addBusy" @click="addMember">Add</v-btn>
          </div>
        </v-card-text>
      </v-card>
    </template>

    <v-dialog v-model="createDialogOpen" max-width="640">
      <v-card title="New Document">
        <v-card-text>
          <v-alert v-if="createDocumentError" type="error" :text="createDocumentError" class="mb-4" />
          <v-select
            id="new-document-category"
            v-model="newCategoryId"
            :items="categories"
            item-title="name"
            item-value="id"
            label="Category"
            class="mb-2"
          />
          <v-text-field id="new-document-title" v-model="newTitle" label="Title" class="mb-2" />
          <v-select
            id="new-document-source-template"
            :model-value="newSourceTemplateId"
            :items="templates"
            item-title="name"
            item-value="id"
            label="Start from a template (optional)"
            clearable
            class="mb-2"
            @update:model-value="onTemplateSelected"
          />
          <v-textarea id="new-document-content" v-model="newContent" label="Content (Markdown)" rows="10" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="createDialogOpen = false">Cancel</v-btn>
          <v-btn id="confirm-create-document" color="primary" :loading="creatingDocument" @click="createDocument">
            Create
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>
