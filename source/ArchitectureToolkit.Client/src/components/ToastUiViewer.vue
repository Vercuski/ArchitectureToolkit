<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import Viewer from '@toast-ui/editor/dist/toastui-editor-viewer'
import '@toast-ui/editor/dist/toastui-editor-viewer.css'
import { interceptAttachmentLinks, patchAttachmentImages } from '@/composables/useAttachmentRendering'

const props = defineProps<{ source: string }>()

const viewerContainer = ref<HTMLElement | null>(null)
// The dedicated Viewer build (not the full Editor with a `viewer: true`
// option) is used deliberately: it never constructs a toolbar in the
// first place, rather than one that's constructed and then hidden.
let viewer: Viewer | null = null
let cleanupLinkInterception: (() => void) | null = null

onMounted(() => {
  if (viewerContainer.value) {
    viewer = new Viewer({
      el: viewerContainer.value,
      initialValue: props.source,
    })
    // A plain <img src>/<a href> can't carry the bearer token
    // attachments require — see useAttachmentRendering.ts for why both
    // of these are necessary, not just one.
    void patchAttachmentImages(viewerContainer.value)
    cleanupLinkInterception = interceptAttachmentLinks(viewerContainer.value)
  }
})

// The markdown being displayed can change without this component being
// unmounted — e.g. DocumentDetailView swapping which revision's content
// is shown in the history dialog — so keep the rendered output in sync.
watch(
  () => props.source,
  (source) => {
    viewer?.setMarkdown(source)
    // setMarkdown re-renders synchronously, so the freshly rendered
    // <img> tags are already in the DOM by the time this runs.
    // interceptAttachmentLinks isn't re-attached here: it's delegated at
    // viewerContainer itself (added once, in onMounted), so it already
    // covers whatever links the new content just rendered.
    if (viewerContainer.value) {
      void patchAttachmentImages(viewerContainer.value)
    }
  },
)

onBeforeUnmount(() => {
  cleanupLinkInterception?.()
  viewer?.destroy()
  viewer = null
})
</script>

<template>
  <div ref="viewerContainer" class="toastui-viewer-host" />
</template>
