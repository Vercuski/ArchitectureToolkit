<script setup lang="ts">
import { computed } from 'vue'
import { marked } from 'marked'
import DOMPurify from 'dompurify'

const props = defineProps<{ source: string }>()

// Template/document content is authored by other users (Architects for
// templates, any Editor/Owner for project documents) — sanitizing before
// rendering as HTML is a real XSS concern, not just defensive boilerplate.
const html = computed(() => DOMPurify.sanitize(marked.parse(props.source, { async: false })))
</script>

<template>
  <div class="markdown-body" v-html="html" />
</template>

<style scoped>
.markdown-body :deep(pre) {
  background: rgba(0, 0, 0, 0.04);
  padding: 12px;
  border-radius: 4px;
  overflow-x: auto;
}
.markdown-body :deep(code) {
  font-family: monospace;
}
.markdown-body :deep(table) {
  border-collapse: collapse;
}
.markdown-body :deep(th),
.markdown-body :deep(td) {
  border: 1px solid rgba(0, 0, 0, 0.12);
  padding: 4px 8px;
}
</style>
