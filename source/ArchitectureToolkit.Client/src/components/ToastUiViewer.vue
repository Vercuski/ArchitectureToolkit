<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import Viewer from '@toast-ui/editor/dist/toastui-editor-viewer'
import '@toast-ui/editor/dist/toastui-editor-viewer.css'

const props = defineProps<{ source: string }>()

const viewerContainer = ref<HTMLElement | null>(null)
// The dedicated Viewer build (not the full Editor with a `viewer: true`
// option) is used deliberately: it never constructs a toolbar in the
// first place, rather than one that's constructed and then hidden.
let viewer: Viewer | null = null

onMounted(() => {
  if (viewerContainer.value) {
    viewer = new Viewer({
      el: viewerContainer.value,
      initialValue: props.source,
    })
  }
})

// The markdown being displayed can change without this component being
// unmounted — e.g. DocumentDetailView swapping which revision's content
// is shown in the history dialog — so keep the rendered output in sync.
watch(
  () => props.source,
  (source) => {
    viewer?.setMarkdown(source)
  },
)

onBeforeUnmount(() => {
  viewer?.destroy()
  viewer = null
})
</script>

<template>
  <div ref="viewerContainer" class="toastui-viewer-host" />
</template>
