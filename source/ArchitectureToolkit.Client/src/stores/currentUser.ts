import { defineStore } from 'pinia'
import { ref } from 'vue'
import { usersApi } from '@/api/users'
import type { UserDto } from '@/api/types'

export const useCurrentUserStore = defineStore('currentUser', () => {
  const profile = ref<UserDto | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  /** Cheap to call from multiple views on mount — only fetches once. */
  async function ensureLoaded() {
    if (profile.value || loading.value) {
      return
    }
    loading.value = true
    error.value = null
    try {
      profile.value = await usersApi.me()
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load your profile.'
    } finally {
      loading.value = false
    }
  }

  return { profile, loading, error, ensureLoaded }
})
