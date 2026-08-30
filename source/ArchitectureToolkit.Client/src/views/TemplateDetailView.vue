<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { templatesApi } from '@/api/templates'
import { ApiError } from '@/api/httpClient'
import { useCurrentUserStore } from '@/stores/currentUser'
import MarkdownView from '@/components/MarkdownView.vue'
import type { BumpType, TemplateDetailDto, TemplateRevisionDetailDto, TemplateRevisionDto } from '@/api/types'

const route = useRoute()
const currentUser = useCurrentUserStore()
const templateId = route.params.id as string

const template = ref<TemplateDetailDto | null>(null)
const revisions = ref<TemplateRevisionDto[]>([])
const loading = ref(true)
const loadError = ref<string | null>(null)

const isArchitect = computed(() => currentUser.profile?.systemRole === 'Architect')

const bumpTypeOptions: BumpType[] = ['Major', 'Minor', 'Patch']

const reviseDialogOpen = ref(false)
const newContent = ref('')
const newBumpType = ref<BumpType>('Minor')
const revising = ref(false)
const reviseError = ref<string | null>(null)

const viewRevision = ref<TemplateRevisionDetailDto | null>(null)
const viewRevisionLoading = ref(false)

async function load() {
  loading.value = true
  loadError.value = null
  try {
    const [templateResult, revisionsResult] = await Promise.all([
      templatesApi.get(templateId),
      templatesApi.listRevisions(templateId),
    ])
    template.value = templateResult
    revisions.value = revisionsResult
  } catch (err) {
    loadError.value = err instanceof Error ? err.message : 'Failed to load template.'
  } finally {
    loading.value = false
  }
}

function openReviseDialog() {
  newContent.value = template.value?.content ?? ''
  newBumpType.value = 'Minor'
  reviseError.value = null
  reviseDialogOpen.value = true
}

async function createRevision() {
  if (!template.value || !newContent.value.trim()) {
    return
  }
  revising.value = true
  reviseError.value = null
  try {
    await templatesApi.createRevision(
      templateId,
      template.value.currentRevisionId,
      newBumpType.value,
      newContent.value,
    )
    reviseDialogOpen.value = false
    await load()
  } catch (err) {
    if (err instanceof ApiError && err.status === 409) {
      // Someone else saved a revision after this template was loaded, so
      // template.currentRevisionId is now stale — retrying with the same
      // value would just conflict again. Refresh it, but leave newContent
      // and the open dialog alone: the user wrote real content here, and
      // silently discarding it would be worse than the conflict itself.
      await load()
      reviseError.value =
        'Someone else saved a new revision of this template while you were editing. ' +
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
    viewRevision.value = await templatesApi.getRevision(templateId, revisionId)
  } finally {
    viewRevisionLoading.value = false
  }
}

onMounted(() => {
  currentUser.ensureLoaded()
  load()
})
</script>

<template>
  <v-container>
    <v-progress-linear v-if="loading" indeterminate class="mb-4" />
    <v-alert v-if="loadError" type="error" :text="loadError" />

    <template v-if="!loading && !loadError && template">
      <div class="d-flex align-center justify-space-between mb-4">
        <div>
          <h1 class="text-h5">{{ template.name }}</h1>
          <span class="text-caption">v{{ template.currentVersion }}</span>
        </div>
        <v-btn v-if="isArchitect" id="new-revision-button" color="accent" @click="openReviseDialog">
          New Revision
        </v-btn>
      </div>

      <v-card class="mb-4">
        <v-card-text>
          <MarkdownView :source="template.content" />
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
            id="new-revision-bump-type"
            v-model="newBumpType"
            :items="bumpTypeOptions"
            label="Bump type"
            class="mb-2"
          />
          <v-textarea id="new-revision-content" v-model="newContent" label="Content (Markdown)" rows="10" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="reviseDialogOpen = false">Cancel</v-btn>
          <v-btn id="confirm-create-revision" color="accent" :loading="revising" @click="createRevision">
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
