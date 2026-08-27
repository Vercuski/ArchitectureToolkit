<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    await authStore.completeLogin()
    await router.push('/')
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Sign-in failed.'
  }
})
</script>

<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-alert v-if="error" type="error" :text="error" />
      <v-progress-circular v-else indeterminate color="primary" size="64" />
    </v-row>
  </v-container>
</template>
