<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { projectsApi } from '@/api/projects'
import { ApiError } from '@/api/httpClient'
import type { ProjectDto } from '@/api/types'

const router = useRouter()

const projects = ref<ProjectDto[]>([])
const loading = ref(true)
const loadError = ref<string | null>(null)

const createDialogOpen = ref(false)
const newProjectName = ref('')
const creating = ref(false)
const createError = ref<string | null>(null)

async function loadProjects() {
  loading.value = true
  loadError.value = null
  try {
    projects.value = await projectsApi.list()
  } catch (err) {
    loadError.value = err instanceof Error ? err.message : 'Failed to load projects.'
  } finally {
    loading.value = false
  }
}

async function createProject() {
  if (!newProjectName.value.trim()) {
    return
  }
  creating.value = true
  createError.value = null
  try {
    const created = await projectsApi.create(newProjectName.value.trim())
    createDialogOpen.value = false
    newProjectName.value = ''
    await router.push(`/projects/${created.id}`)
  } catch (err) {
    createError.value =
      err instanceof ApiError
        ? ((err.body as { error?: string })?.error ?? 'Failed to create project.')
        : 'Failed to create project.'
  } finally {
    creating.value = false
  }
}

onMounted(loadProjects)
</script>

<template>
  <v-container>
    <div class="d-flex align-center justify-space-between mb-4">
      <h1 class="text-h5">Projects</h1>
        <v-btn color="primary" prepend-icon="mdi-plus" @click="createDialogOpen = true">
          New Project
        </v-btn>
    </div>

    <v-alert v-if="loadError" type="error" :text="loadError" class="mb-4" />
    <v-progress-linear v-if="loading" indeterminate class="mb-4" />

    <v-alert v-if="!loading && !loadError && projects.length === 0" type="info" variant="tonal">
      No projects yet — create your first one to get started.
    </v-alert>

    <v-list v-else lines="one">
      <v-list-item
        v-for="project in projects"
        :key="project.id"
        :title="project.name"
        prepend-icon="mdi-folder-outline"
        @click="router.push(`/projects/${project.id}`)"
      />
    </v-list>

    <v-dialog v-model="createDialogOpen" max-width="480">
      <v-card title="New Project">
        <v-card-text>
          <v-alert v-if="createError" type="error" :text="createError" class="mb-4" />
          <v-text-field
            id="new-project-name"
            v-model="newProjectName"
            label="Project name"
            autofocus
            @keyup.enter="createProject"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="createDialogOpen = false">Cancel</v-btn>
          <v-btn id="confirm-create-project" color="primary" :loading="creating" @click="createProject">Create</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>
