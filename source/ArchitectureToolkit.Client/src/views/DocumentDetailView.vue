<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { documentsApi } from '@/api/documents'
import { projectsApi } from '@/api/projects'
import { categoriesApi } from '@/api/categories'
import { ApiError } from '@/api/httpClient'
import { useAuthStore } from '@/stores/auth'
import MarkdownView from '@/components/MarkdownView.vue'
import type {
  BumpType,
  CategoryDto,
  DocumentRevisionDetailDto,
  DocumentRevisionDto,
  ProjectDocumentDetailDto,
} from '@/api/types'

const route = useRoute()
const authStore = useAuthStore()
const documentId = route.params.id as string

const document = ref<ProjectDocumentDetailDto | null>(null)
const revisions = ref<DocumentRevisionDto[]>([])
const categories = ref<CategoryDto[]>([])
const loading = ref(true)
const loadError = ref<string | null>(null)

// Document identity is global (~/api/documents/{id}), not nested under a
// project in the route, so — unlike ProjectDetailView, which already has
// the member list loaded — the caller's role here has to be resolved
// from the document's own projectId once it's known. Same email-matching
// reasoning as ProjectDetailView's myMembership.
const myEmail = computed(() => authStore.user?.profile.email)
const myRole = ref<string | null>(null)
const canEdit = computed(() => myRole.value === 'Editor' || myRole.value === 'Owner')

const categoryName = computed(
  () => categories.value.find((c) => c.id === document.value?.categoryId)?.name ?? 'Uncategorized',
)

const bumpTypeOptions: BumpType[] = ['Major', 'Minor', 'Patch']

const reviseDialogOpen = ref(false)
const newContent = ref('')
const newBumpType = ref<BumpType>('Minor')
const revising = ref(false)
const reviseError = ref<string | null>(null)

const viewRevision = ref<DocumentRevisionDetailDto | null>(null)
const viewRevisionLoading = ref(false)

async function load() {
  loading.value = true
  loadError.value = null
  try {
    const documentResult = await documentsApi.get(documentId)
    document.value = documentResult

    const [revisionsResult, categoriesResult, membersResult] = await Promise.all([
      documentsApi.listRevisions(documentId),
      categoriesApi.list(),
      projectsApi.listMembers(documentResult.projectId),
    ])
    revisions.value = revisionsResult
    categories.value = categoriesResult
    myRole.value = membersResult.find((m) => m.userEmail === myEmail.value)?.role ?? null
  } catch (err) {
    loadError.value = err instanceof Error ? err.message : 'Failed to load document.'
  } finally {
    loading.value = false
  }
}

function openReviseDialog() {
  newContent.value = document.value?.content ?? ''
  newBumpType.value = 'Minor'
  reviseError.value = null
  reviseDialogOpen.value = true
}

async function createRevision() {
  if (!document.value || !newContent.value.trim()) {
    return
  }
  revising.value = true
  reviseError.value = null
  try {
    await documentsApi.createRevision(
      documentId,
      document.value.currentRevisionId,
      newBumpType.value,
      newContent.value,
    )
    reviseDialogOpen.value = false
    await load()
  } catch (err) {
    if (err instanceof ApiError && err.status === 409) {
      // Same conflict pattern as TemplateDetailView: refresh so
      // currentRevisionId is no longer stale, but keep the draft and
      // the dialog open rather than discarding what the user wrote.
      await load()
      reviseError.value =
        'Someone else saved a new revision of this document while you were editing. ' +
        'Review the latest content behind this dialog, then Save again to reapply your changes.'
    } else {
      reviseError.value =
        err instanceof ApiError
          ? ((err.body as { error?: string })?.error ?? 'Failed to create revision.')
          : 'Failed to create revision.'
    }
  } finally {
    revising.value = false
  }
}

async function openRevision(revisionId: string) {
  viewRevisionLoading.value = true
  try {
    viewRevision.value = await documentsApi.getRevision(documentId, revisionId)
  } finally {
    viewRevisionLoading.value = false
  }
}

onMounted(load)
</script>

<template>
  <v-container>
    <v-progress-linear v-if="loading" indeterminate class="mb-4" />
    <v-alert v-if="loadError" type="error" :text="loadError" />

    <template v-if="!loading && !loadError && document">
      <v-btn
        variant="text"
        prepend-icon="mdi-arrow-left"
        class="mb-2"
        :to="`/projects/${document.projectId}`"
      >
        Back to project
      </v-btn>

      <div class="d-flex align-center justify-space-between mb-4">
        <div>
          <h1 class="text-h5">{{ document.title }}</h1>
          <span class="text-caption">{{ categoryName }} · v{{ document.currentVersion }}</span>
          <v-chip v-if="document.sourceTemplateRevisionId" size="x-small" class="ml-2" variant="tonal">
            Started from a template
          </v-chip>
        </div>
        <v-btn v-if="canEdit" id="new-document-revision-button" color="primary" @click="openReviseDialog">
          New Revision
        </v-btn>
      </div>

      <v-card class="mb-4">
        <v-card-text>
          <MarkdownView :source="document.content" />
        </v-card-text>
      </v-card>

      <v-card title="Revision History">
        <v-table>
          <thead>
            <tr>
              <th>Version</th>
              <th>Bump</th>
              <th>Created</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="revision in revisions"
              :key="revision.id"
              style="cursor: pointer"
              @click="openRevision(revision.id)"
            >
              <td>{{ revision.version }}</td>
              <td>{{ revision.bumpType ?? '—' }}</td>
              <td>{{ new Date(revision.createdAt).toLocaleString() }}</td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <v-dialog v-model="reviseDialogOpen" max-width="640">
      <v-card title="New Revision">
        <v-card-text>
          <v-alert v-if="reviseError" type="error" :text="reviseError" class="mb-4" />
          <v-select
            id="new-document-revision-bump-type"
            v-model="newBumpType"
            :items="bumpTypeOptions"
            label="Bump type"
            class="mb-2"
          />
          <v-textarea id="new-document-revision-content" v-model="newContent" label="Content (Markdown)" rows="10" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="reviseDialogOpen = false">Cancel</v-btn>
          <v-btn id="confirm-create-document-revision" color="primary" :loading="revising" @click="createRevision">
            Save
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog :model-value="!!viewRevision" max-width="640" @update:model-value="viewRevision = null">
      <v-card v-if="viewRevision" :title="`Version ${viewRevision.version}`">
        <v-card-text>
          <MarkdownView :source="viewRevision.content" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="viewRevision = null">Close</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>
