<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useCurrentUserStore } from '@/stores/currentUser'

const currentUser = useCurrentUserStore()
const copied = ref(false)

async function copyUserId() {
  if (!currentUser.profile) return
  await navigator.clipboard.writeText(currentUser.profile.id)
  copied.value = true
  setTimeout(() => (copied.value = false), 1500)
}

onMounted(() => currentUser.ensureLoaded())
</script>

<template>
  <v-container>
    <v-card class="mb-4">
      <v-card-title>Welcome to ArchitectureToolkit</v-card-title>
      <v-card-text>
        <v-alert v-if="currentUser.error" type="error" :text="currentUser.error" />
        <template v-else-if="currentUser.profile">
          <p>{{ currentUser.profile.name }} ({{ currentUser.profile.email }})</p>
          <p class="text-caption mt-2">
            Your User ID — share this with a project Owner so they can add you:
          </p>
          <div class="d-flex align-center ga-2 mt-1">
            <code>{{ currentUser.profile.id }}</code>
            <v-btn size="small" variant="text" @click="copyUserId">
              {{ copied ? 'Copied!' : 'Copy' }}
            </v-btn>
          </div>
        </template>
      </v-card-text>
    </v-card>

    <div class="d-flex ga-2">
      <v-btn color="primary" prepend-icon="mdi-folder-multiple-outline" to="/projects">
        Go to Projects
      </v-btn>
      <v-btn color="primary" variant="tonal" prepend-icon="mdi-file-document-multiple-outline" to="/templates">
        Browse Templates
      </v-btn>
    </div>
  </v-container>
</template>
