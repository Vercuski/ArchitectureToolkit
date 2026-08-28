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

  /**
   * Redirects the browser to /connect/authorize — never returns.
   * `returnUrl` round-trips through oidc-client-ts's own `state` handling
   * (not a query param we control), so the router guard's deep link
   * survives the OIDC redirect without the backend needing to know about it.
   */
  async function login(returnUrl?: string) {
    await userManager.signinRedirect(returnUrl ? { state: { returnUrl } } : undefined)
  }

  /**
   * Called from CallbackView once /auth/callback receives the redirect
   * back. Returns where the caller should navigate next: the original
   * deep link if `login` was given one, otherwise '/'.
   */
  async function completeLogin(): Promise<string> {
    const loadedUser = await userManager.signinRedirectCallback()
    user.value = loadedUser
    const state = loadedUser.state as { returnUrl?: string } | undefined
    return state?.returnUrl ?? '/'
  }

  /** Redirects the browser to end the session at the provider too. */
  async function logout() {
    await userManager.signoutRedirect()
  }

  return { user, isAuthenticated, accessToken, initialize, login, completeLogin, logout }
})
