import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { User } from 'oidc-client-ts'

vi.mock('@/auth/oidcConfig', () => ({
  userManager: {
    getUser: vi.fn().mockResolvedValue(null),
    signinRedirect: vi.fn().mockResolvedValue(undefined),
    signinRedirectCallback: vi.fn(),
    signoutRedirect: vi.fn(),
    events: { addUserLoaded: vi.fn(), addUserUnloaded: vi.fn() },
  },
}))

// Stubbed out so importing router/index.ts doesn't drag the real Vuetify
// component tree (and its CSS) into this guard-only test — the guard
// itself is what's under test here, not any individual view's rendering.
const stubComponent = { render: () => null }
vi.mock('../../views/HomeView.vue', () => ({ default: stubComponent }))
vi.mock('../../views/CallbackView.vue', () => ({ default: stubComponent }))
vi.mock('../../views/ProjectListView.vue', () => ({ default: stubComponent }))
vi.mock('../../views/ProjectDetailView.vue', () => ({ default: stubComponent }))
vi.mock('../../views/TemplateListView.vue', () => ({ default: stubComponent }))
vi.mock('../../views/TemplateDetailView.vue', () => ({ default: stubComponent }))
vi.mock('../../views/DocumentDetailView.vue', () => ({ default: stubComponent }))
vi.mock('../../views/UserManagementView.vue', () => ({ default: stubComponent }))
vi.mock('../../views/SetPasswordView.vue', () => ({ default: stubComponent }))

// Imported after the mock above, and in this order: the auth store first so
// the spied instance below is the same one router/index.ts's guard resolves
// via useAuthStore() (both go through the same active Pinia).
const { useAuthStore } = await import('@/stores/auth')
const { default: router } = await import('../index')

function signIn(authStore: ReturnType<typeof useAuthStore>) {
  // isAuthenticated is derived from `user` — set it directly rather than
  // exercising oidc-client-ts's session-loading path, which
  // stores/auth.spec.ts already covers on its own.
  authStore.user = { access_token: 'token-123', expired: false } as User
}

describe('router auth guard', () => {
  beforeEach(async () => {
    setActivePinia(createPinia())
    await router.replace('/')
  })

  it('blocks navigation to a protected route and redirects to login when signed out', async () => {
    const authStore = useAuthStore()
    const loginSpy = vi.spyOn(authStore, 'login').mockResolvedValue(undefined)

    await router.push('/templates/abc')

    expect(loginSpy).toHaveBeenCalledWith('/templates/abc')
    expect(router.currentRoute.value.fullPath).not.toBe('/templates/abc')
  })

  it('allows navigation to a protected route when signed in', async () => {
    const authStore = useAuthStore()
    signIn(authStore)
    const loginSpy = vi.spyOn(authStore, 'login')

    await router.push('/templates/abc')

    expect(loginSpy).not.toHaveBeenCalled()
    expect(router.currentRoute.value.fullPath).toBe('/templates/abc')
  })

  it('allows navigation to the auth callback route regardless of auth state', async () => {
    const authStore = useAuthStore()
    const loginSpy = vi.spyOn(authStore, 'login')

    await router.push('/auth/callback')

    expect(loginSpy).not.toHaveBeenCalled()
    expect(router.currentRoute.value.fullPath).toBe('/auth/callback')
  })
})
