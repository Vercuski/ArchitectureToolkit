<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { templatesApi } from '@/api/templates'
import { useCurrentUserStore } from '@/stores/currentUser'
import ToastUiViewer from '@/components/ToastUiViewer.vue'
import type { TemplateDetailDto, TemplateRevisionDetailDto, TemplateRevisionDto } from '@/api/types'

const route = useRoute()
const currentUser = useCurrentUserStore()
const templateId = route.params.id as string

const template = ref<TemplateDetailDto | null>(null)
const revisions = ref<TemplateRevisionDto[]>([])
const loading = ref(true)
const loadError = ref<string | null>(null)

const isArchitect = computed(() => currentUser.profile?.systemRole === 'Architect')

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
        <v-btn v-if="isArchitect" id="new-revision-button" color="accent" :to="`/templates/${templateId}/revise`">
          New Revision
        </v-btn>
      </div>

      <v-card class="mb-4">
        <v-card-text>
          <ToastUiViewer :source="template.content" />
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

    <v-dialog :model-value="!!viewRevision" max-width="640" @update:model-value="viewRevision = null">
      <v-card v-if="viewRevision" :title="`Version ${viewRevision.version}`">
        <v-card-text>
          <ToastUiViewer :source="viewRevision.content" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="viewRevision = null">Close</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>
