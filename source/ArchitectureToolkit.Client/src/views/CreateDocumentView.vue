<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Editor from '@toast-ui/editor'
import '@toast-ui/editor/dist/toastui-editor.css'
import { documentsApi } from '@/api/documents'
import { templatesApi } from '@/api/templates'
import { categoriesApi } from '@/api/categories'
import { ApiError } from '@/api/httpClient'
import type { CategoryDto, TemplateDetailDto, TemplateSummaryDto } from '@/api/types'

const route = useRoute()
const router = useRouter()
const projectId = route.params.projectId as string

const categories = ref<CategoryDto[]>([])
const templates = ref<TemplateSummaryDto[]>([])
const loading = ref(true)
const loadError = ref<string | null>(null)

const selectedCategoryId = ref<string | null>(null)
const selectedTemplateId = ref<string | null>(null)
// Cached from the template fetch that already ran to prefill the editor —
// createDocument reuses currentRevisionId from here instead of refetching
// the same template a second time on submit.
const selectedTemplateDetail = ref<TemplateDetailDto | null>(null)
const title = ref('')

const creating = ref(false)
const createError = ref<string | null>(null)

const editorContainer = ref<HTMLElement | null>(null)
let editor: Editor | null = null
// Snapshot of the editor's content right after the last programmatic
// setMarkdown call (initial mount, or a template selection) — compared
// against the live content at cancel-time to decide whether the user has
// actually typed something worth confirming before discarding.
let editorBaseline = ''

const cancelConfirmOpen = ref(false)

// Only templates belonging to the selected category — the whole point of
// the two dropdowns being paired rather than independent.
const filteredTemplates = computed(() =>
  selectedCategoryId.value
    ? templates.value.filter((t) => t.categoryId === selectedCategoryId.value)
    : [],
)

// If a category change leaves the current template selection pointing at
// a template from a different category, drop it rather than show an
// inconsistent pairing.
watch(selectedCategoryId, () => {
  if (selectedTemplateId.value && !filteredTemplates.value.some((t) => t.id === selectedTemplateId.value)) {
    selectedTemplateId.value = null
    selectedTemplateDetail.value = null
  }
})

async function load() {
  loading.value = true
  loadError.value = null
  try {
    const [categoriesResult, templatesResult] = await Promise.all([categoriesApi.list(), templatesApi.list()])
    categories.value = categoriesResult
    templates.value = templatesResult
  } catch (err) {
    loadError.value = err instanceof Error ? err.message : 'Failed to load categories and templates.'
  } finally {
    loading.value = false
  }
}

// Prefills the editor from the chosen template's current content, but
// leaves it fully editable afterward — picking a starting point, not
// locking the user into it.
async function onTemplateSelected(templateId: string | null) {
  selectedTemplateId.value = templateId
  if (!templateId) {
    selectedTemplateDetail.value = null
    editor?.setMarkdown('')
    editorBaseline = ''
    return
  }
  try {
    const template = await templatesApi.get(templateId)
    selectedTemplateDetail.value = template
    editor?.setMarkdown(template.content)
    editorBaseline = template.content
    if (!selectedCategoryId.value) {
      selectedCategoryId.value = template.categoryId
    }
  } catch (err) {
    createError.value = err instanceof Error ? err.message : 'Failed to load template content.'
  }
}

async function createDocument() {
  if (!selectedCategoryId.value || !title.value.trim() || !editor) {
    return
  }
  creating.value = true
  createError.value = null
  try {
    const sourceTemplateRevisionId = selectedTemplateDetail.value?.currentRevisionId ?? null
    const created = await documentsApi.create(
      projectId,
      selectedCategoryId.value,
      title.value.trim(),
      editor.getMarkdown(),
      sourceTemplateRevisionId,
    )
    await router.push(`/documents/${created.id}`)
  } catch (err) {
    createError.value =
      err instanceof ApiError ? ((err.body as { error?: string })?.error ?? 'Failed to create document.') : 'Failed to create document.'
  } finally {
    creating.value = false
  }
}

function hasUnsavedEditorChanges(): boolean {
  return editor !== null && editor.getMarkdown() !== editorBaseline
}

function cancel() {
  if (hasUnsavedEditorChanges()) {
    cancelConfirmOpen.value = true
    return
  }
  router.push(`/projects/${projectId}`)
}

function confirmDiscard() {
  cancelConfirmOpen.value = false
  router.push(`/projects/${projectId}`)
}

onMounted(async () => {
  await load()
  if (editorContainer.value) {
    editor = new Editor({
      el: editorContainer.value,
      height: '500px',
      // The literal request this page implements: raw markdown source on
      // the left, live-rendered preview on the right — not the WYSIWYG
      // edit type, which hides the raw source entirely.
      initialEditType: 'markdown',
      previewStyle: 'vertical',
      initialValue: '',
    })
  }
})

onBeforeUnmount(() => {
  editor?.destroy()
  editor = null
})
</script>

<template>
  <v-container fluid>
    <v-progress-linear v-if="loading" indeterminate class="mb-4" />
    <v-alert v-if="loadError" type="error" :text="loadError" class="mb-4" />

    <template v-if="!loading && !loadError">
      <h1 class="text-h5 mb-4">New Document</h1>
      <v-alert v-if="createError" type="error" :text="createError" class="mb-4" />

      <v-row>
        <v-col cols="12" sm="6">
          <v-select
            id="new-document-category"
            v-model="selectedCategoryId"
            :items="categories"
            item-title="name"
            item-value="id"
            label="Category"
          />
        </v-col>
        <v-col cols="12" sm="6">
          <v-select
            id="new-document-source-template"
            :model-value="selectedTemplateId"
            :items="filteredTemplates"
            item-title="name"
            item-value="id"
            label="Template"
            :disabled="!selectedCategoryId"
            :hint="!selectedCategoryId ? 'Select a category first' : undefined"
            persistent-hint
            clearable
            @update:model-value="onTemplateSelected"
          />
        </v-col>
      </v-row>

      <v-text-field id="new-document-title" v-model="title" label="Title" class="mb-4" />

      <div ref="editorContainer" id="new-document-editor" class="mb-6"></div>

      <div class="d-flex justify-end ga-2">
        <v-btn id="cancel-create-document" variant="text" @click="cancel">Cancel</v-btn>
        <v-btn
          id="confirm-create-document"
          color="accent"
          :loading="creating"
          :disabled="!selectedCategoryId || !title.trim()"
          @click="createDocument"
        >
          Create
        </v-btn>
      </div>
    </template>

    <v-dialog v-model="cancelConfirmOpen" max-width="400">
      <v-card title="Discard changes?">
        <v-card-text>
          You have unsaved changes to this document's content. Are you sure you want to discard them?
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="cancelConfirmOpen = false">Keep Editing</v-btn>
          <v-btn id="confirm-discard-changes" color="error" @click="confirmDiscard">Discard</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>
