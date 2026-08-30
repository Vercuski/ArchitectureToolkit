<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Editor from '@toast-ui/editor'
import '@toast-ui/editor/dist/toastui-editor.css'
import { templatesApi } from '@/api/templates'
import { ApiError } from '@/api/httpClient'
import type { BumpType, TemplateDetailDto } from '@/api/types'

const route = useRoute()
const router = useRouter()
const templateId = route.params.id as string

const template = ref<TemplateDetailDto | null>(null)
const loading = ref(true)
const loadError = ref<string | null>(null)

const bumpType = ref<BumpType>('Minor')
const bumpTypeOptions: BumpType[] = ['Major', 'Minor', 'Patch']

const saving = ref(false)
const saveError = ref<string | null>(null)

const cancelConfirmOpen = ref(false)

const editorContainer = ref<HTMLElement | null>(null)
let editor: Editor | null = null
// Snapshot of the editor's content right after it's created from the
// template's current content — compared against the live content at
// cancel-time to decide whether there's actually something to confirm
// discarding.
let editorBaseline = ''

async function load() {
  loading.value = true
  loadError.value = null
  try {
    template.value = await templatesApi.get(templateId)
  } catch (err) {
    loadError.value = err instanceof Error ? err.message : 'Failed to load template.'
  } finally {
    loading.value = false
  }
}

async function save() {
  if (!template.value || !editor) {
    return
  }
  saving.value = true
  saveError.value = null
  try {
    await templatesApi.createRevision(
      templateId,
      template.value.currentRevisionId,
      bumpType.value,
      editor.getMarkdown(),
    )
    await router.push(`/templates/${templateId}`)
  } catch (err) {
    if (err instanceof ApiError && err.status === 409) {
      // Someone else saved a revision since this page loaded, so
      // template.currentRevisionId is now stale — retrying with the same
      // value would just conflict again. Refresh it, but leave the
      // editor's content alone: the user wrote this, and discarding it
      // would be worse than the conflict itself.
      await load()
      saveError.value =
        'Someone else saved a new revision of this template while you were editing. ' +
        'Review the latest content, then Save again to reapply your changes.'
    } else {
      saveError.value =
        err instanceof ApiError ? ((err.body as { error?: string })?.error ?? 'Failed to create revision.') : 'Failed to create revision.'
    }
  } finally {
    saving.value = false
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
  router.push(`/templates/${templateId}`)
}

function confirmDiscard() {
  cancelConfirmOpen.value = false
  router.push(`/templates/${templateId}`)
}

onMounted(async () => {
  await load()
  if (editorContainer.value && template.value) {
    editor = new Editor({
      el: editorContainer.value,
      height: '500px',
      // The literal request this page implements: raw markdown source on
      // the left, live-rendered preview on the right — not the WYSIWYG
      // edit type, which hides the raw source entirely.
      initialEditType: 'markdown',
      previewStyle: 'vertical',
      initialValue: template.value.content,
    })
    editorBaseline = template.value.content
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

    <template v-if="!loading && !loadError && template">
      <h1 class="text-h5 mb-4">New Revision — {{ template.name }}</h1>
      <v-alert v-if="saveError" type="error" :text="saveError" class="mb-4" />

      <v-select
        id="new-revision-bump-type"
        v-model="bumpType"
        :items="bumpTypeOptions"
        label="Bump type"
        class="mb-4"
        style="max-width: 300px"
      />

      <div ref="editorContainer" id="revise-template-editor" class="mb-6"></div>

      <div class="d-flex justify-end ga-2">
        <v-btn id="cancel-revise-template" variant="text" @click="cancel">Cancel</v-btn>
        <v-btn id="confirm-revise-template" color="accent" :loading="saving" @click="save">Save</v-btn>
      </div>
    </template>

    <v-dialog v-model="cancelConfirmOpen" max-width="400">
      <v-card title="Discard changes?">
        <v-card-text>
          You have unsaved changes to this revision's content. Are you sure you want to discard them?
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
