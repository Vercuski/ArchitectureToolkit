<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { usersApi } from '@/api/users'
import type { UserDto } from '@/api/types'

const me = ref<UserDto | null>(null)
const loadError = ref<string | null>(null)
const copied = ref(false)

async function copyUserId() {
  if (!me.value) return
  await navigator.clipboard.writeText(me.value.id)
  copied.value = true
  setTimeout(() => (copied.value = false), 1500)
}

onMounted(async () => {
  try {
    me.value = await usersApi.me()
  } catch (err) {
    loadError.value = err instanceof Error ? err.message : 'Failed to load your profile.'
  }
})
</script>

<template>
  <v-container>
    <v-card class="mb-4">
      <v-card-title>Welcome to ArchitectureToolkit</v-card-title>
      <v-card-text>
        <v-alert v-if="loadError" type="error" :text="loadError" />
        <template v-else-if="me">
          <p>{{ me.name }} ({{ me.email }})</p>
          <p class="text-caption mt-2">
            Your User ID — share this with a project Owner so they can add you:
          </p>
          <div class="d-flex align-center ga-2 mt-1">
            <code>{{ me.id }}</code>
            <v-btn size="small" variant="text" @click="copyUserId">
              {{ copied ? 'Copied!' : 'Copy' }}
            </v-btn>
          </div>
        </template>
      </v-card-text>
    </v-card>

    <v-btn color="primary" prepend-icon="mdi-folder-multiple-outline" to="/projects">
      Go to Projects
    </v-btn>
  </v-container>
</template>
