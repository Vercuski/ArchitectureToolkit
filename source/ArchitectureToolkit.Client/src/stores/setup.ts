import { defineStore } from 'pinia'
import { ref } from 'vue'
import { setupApi } from '@/api/setup'

export const useSetupStore = defineStore('setup', () => {
  // Starts optimistic (true) so an already-configured deployment never
  // flashes the wizard while checkStatus() resolves — awaited in main.ts
  // before the router's first navigation, same pattern as
  // useAuthStore.initialize().
  const isConfigured = ref(true)

  async function checkStatus() {
    try {
      const status = await setupApi.status()
      isConfigured.value = status.isConfigured
    } catch {
      // A failed status check (e.g. a network hiccup on first paint)
      // fails open to "configured" rather than trapping a perfectly
      // working deployment behind a wizard it doesn't need — the
      // router guard re-checks on every navigation regardless, so a
      // genuinely unconfigured deployment still reaches /setup on the
      // very next route change.
      isConfigured.value = true
    }
  }

  /** Called once the wizard's poll confirms the app is back up. */
  function markConfigured() {
    isConfigured.value = true
  }

  return { isConfigured, checkStatus, markConfigured }
})
