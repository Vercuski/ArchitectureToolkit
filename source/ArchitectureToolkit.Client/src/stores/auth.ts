import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import type { User } from 'oidc-client-ts'
import { userManager } from '@/auth/oidcConfig'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)

  const isAuthenticated = computed(() => !!user.value && !user.value.expired)
  const accessToken = computed(() => user.value?.access_token ?? null)

  /**
   * Must be awaited before the app's first route resolves — see main.ts.
   * Loads any existing session from storage and keeps `user` in sync with
   * oidc-client-ts's own events (token refreshed via automaticSilentRenew,
   * or the session ending for any reason).
   */
  async function initialize() {
    user.value = await userManager.getUser()
    userManager.events.addUserLoaded((loadedUser) => {
      user.value = loadedUser
    })
    userManager.events.addUserUnloaded(() => {
      user.value = null
    })
  }

  /** Redirects the browser to /connect/authorize — never returns. */
  async function login() {
    await userManager.signinRedirect()
  }

  /** Called from CallbackView once /auth/callback receives the redirect back. */
  async function completeLogin() {
    user.value = await userManager.signinRedirectCallback()
  }

  /** Redirects the browser to end the session at the provider too. */
  async function logout() {
    await userManager.signoutRedirect()
  }

  return { user, isAuthenticated, accessToken, initialize, login, completeLogin, logout }
})
