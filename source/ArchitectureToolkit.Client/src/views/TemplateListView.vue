<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { templatesApi } from '@/api/templates'
import { categoriesApi } from '@/api/categories'
import { ApiError } from '@/api/httpClient'
import { useCurrentUserStore } from '@/stores/currentUser'
import type { CategoryDto, TemplateSummaryDto } from '@/api/types'

const router = useRouter()
const currentUser = useCurrentUserStore()

const templates = ref<TemplateSummaryDto[]>([])
const categories = ref<CategoryDto[]>([])
const loading = ref(true)
const loadError = ref<string | null>(null)

const isArchitect = computed(() => currentUser.profile?.systemRole === 'Architect')

const categoryName = (categoryId: string) =>
  categories.value.find((c) => c.id === categoryId)?.name ?? 'Uncategorized'

const groupedTemplates = computed(() => {
  const groups = new Map<string, TemplateSummaryDto[]>()
  for (const template of templates.value) {
    const name = categoryName(template.categoryId)
    if (!groups.has(name)) groups.set(name, [])
    groups.get(name)!.push(template)
  }
  return [...groups.entries()].sort(([a], [b]) => a.localeCompare(b))
})

const createDialogOpen = ref(false)
const newCategoryId = ref('')
const newName = ref('')
const newContent = ref('')
const creating = ref(false)
const createError = ref<string | null>(null)

async function load() {
  loading.value = true
  loadError.value = null
  try {
    const [templatesResult, categoriesResult] = await Promise.all([
      templatesApi.list(),
      categoriesApi.list(),
    ])
    templates.value = templatesResult
    categories.value = categoriesResult
  } catch (err) {
    loadError.value = err instanceof Error ? err.message : 'Failed to load templates.'
  } finally {
    loading.value = false
  }
}

async function createTemplate() {
  if (!newCategoryId.value || !newName.value.trim() || !newContent.value.trim()) {
    return
  }
  creating.value = true
  createError.value = null
  try {
    const created = await templatesApi.create(newCategoryId.value, newName.value.trim(), newContent.value)
    createDialogOpen.value = false
    newCategoryId.value = ''
    newName.value = ''
    newContent.value = ''
    await router.push(`/templates/${created.id}`)
  } catch (err) {
    createError.value =
      err instanceof ApiError ? ((err.body as { error?: string })?.error ?? 'Failed to create template.') : 'Failed to create template.'
  } finally {
    creating.value = false
  }
}

onMounted(() => {
  currentUser.ensureLoaded()
  load()
})
</script>

<template>
  <v-container>
    <div class="d-flex align-center justify-space-between mb-4">
      <h1 class="text-h5">Template Library</h1>
      <v-btn
        v-if="isArchitect"
        id="new-template-button"
        color="accent"
        prepend-icon="mdi-plus"
        @click="createDialogOpen = true"
      >
        New Template
      </v-btn>
    </div>

    <v-alert v-if="loadError" type="error" :text="loadError" class="mb-4" />
    <v-progress-linear v-if="loading" indeterminate class="mb-4" />

    <template v-for="[category, items] in groupedTemplates" :key="category">
      <h2 class="text-subtitle-1 mt-4 mb-1">{{ category }}</h2>
      <v-list lines="one">
        <v-list-item
          v-for="template in items"
          :key="template.id"
          :title="template.name"
          :subtitle="`v${template.currentVersion}`"
          prepend-icon="mdi-file-document-outline"
          @click="router.push(`/templates/${template.id}`)"
        />
      </v-list>
    </template>

    <v-dialog v-model="createDialogOpen" max-width="640">
      <v-card title="New Template">
        <v-card-text>
          <v-alert v-if="createError" type="error" :text="createError" class="mb-4" />
          <v-select
            id="new-template-category"
            v-model="newCategoryId"
            :items="categories"
            item-title="name"
            item-value="id"
            label="Category"
            class="mb-2"
          />
          <v-text-field id="new-template-name" v-model="newName" label="Template name" class="mb-2" />
          <v-textarea id="new-template-content" v-model="newContent" label="Content (Markdown)" rows="10" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="createDialogOpen = false">Cancel</v-btn>
          <v-btn id="confirm-create-template" color="accent" :loading="creating" @click="createTemplate">
            Create
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>
