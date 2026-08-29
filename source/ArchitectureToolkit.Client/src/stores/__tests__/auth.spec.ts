import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { User } from 'oidc-client-ts'

const getUserMock = vi.fn()
const signinRedirectMock = vi.fn()
const signinRedirectCallbackMock = vi.fn()
const signoutRedirectMock = vi.fn()
const addUserLoadedMock = vi.fn()
const addUserUnloadedMock = vi.fn()

vi.mock('@/auth/oidcConfig', () => ({
  userManager: {
    getUser: getUserMock,
    signinRedirect: signinRedirectMock,
    signinRedirectCallback: signinRedirectCallbackMock,
    signoutRedirect: signoutRedirectMock,
    events: {
      addUserLoaded: addUserLoadedMock,
      addUserUnloaded: addUserUnloadedMock,
    },
  },
}))

// Imported after the mock above so the store picks up the mocked userManager.
const { useAuthStore } = await import('../auth')

function fakeUser(overrides: Partial<User> = {}): User {
  return {
    access_token: 'token-123',
    expired: false,
    state: undefined,
    profile: {},
    ...overrides,
  } as User
}

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    getUserMock.mockReset().mockResolvedValue(null)
    signinRedirectMock.mockReset().mockResolvedValue(undefined)
    signinRedirectCallbackMock.mockReset()
    signoutRedirectMock.mockReset().mockResolvedValue(undefined)
    addUserLoadedMock.mockReset()
    addUserUnloadedMock.mockReset()
  })

  describe('initialize', () => {
    it('loads any existing session from storage', async () => {
      const user = fakeUser()
      getUserMock.mockResolvedValue(user)

      const store = useAuthStore()
      await store.initialize()

      // toEqual, not toBe — see the note in the events test below.
      expect(store.user).toEqual(user)
      expect(store.isAuthenticated).toBe(true)
    })

    it("keeps user in sync with oidc-client-ts's userLoaded/userUnloaded events", async () => {
      const store = useAuthStore()
      await store.initialize()

      const onLoaded = addUserLoadedMock.mock.calls[0]?.[0] as (u: User) => void
      const onUnloaded = addUserUnloadedMock.mock.calls[0]?.[0] as () => void

      const refreshedUser = fakeUser({ access_token: 'refreshed-token' })
      onLoaded(refreshedUser)
      // toEqual, not toBe: Pinia/Vue wrap an assigned object in a reactive
      // proxy, so store.user is never reference-identical to what was
      // assigned — only deep-equal.
      expect(store.user).toEqual(refreshedUser)

      onUnloaded()
      expect(store.user).toBeNull()
    })
  })

  describe('isAuthenticated', () => {
    it('is false when there is no signed-in user', async () => {
      const store = useAuthStore()
      await store.initialize()

      expect(store.isAuthenticated).toBe(false)
    })

    it('is false for a user whose session has expired', async () => {
      getUserMock.mockResolvedValue(fakeUser({ expired: true }))

      const store = useAuthStore()
      await store.initialize()

      expect(store.isAuthenticated).toBe(false)
    })
  })

  describe('accessToken', () => {
    it('is null when there is no signed-in user', async () => {
      const store = useAuthStore()
      await store.initialize()

      expect(store.accessToken).toBeNull()
    })

    it("reflects the signed-in user's access_token", async () => {
      getUserMock.mockResolvedValue(fakeUser({ access_token: 'token-abc' }))

      const store = useAuthStore()
      await store.initialize()

      expect(store.accessToken).toBe('token-abc')
    })
  })

  describe('login', () => {
    it('redirects without carrying state when no returnUrl is given', async () => {
      const store = useAuthStore()
      await store.login()

      expect(signinRedirectMock).toHaveBeenCalledWith(undefined)
    })

    it('carries returnUrl through as oidc-client-ts state, for CallbackView to read back', async () => {
      const store = useAuthStore()
      await store.login('/templates/abc')

      expect(signinRedirectMock).toHaveBeenCalledWith({ state: { returnUrl: '/templates/abc' } })
    })
  })

  describe('completeLogin', () => {
    it('sets the user and returns the return URL carried in state', async () => {
      signinRedirectCallbackMock.mockResolvedValue(fakeUser({ state: { returnUrl: '/templates/abc' } }))

      const store = useAuthStore()
      const returnUrl = await store.completeLogin()

      expect(store.isAuthenticated).toBe(true)
      expect(returnUrl).toBe('/templates/abc')
    })

    it("defaults to '/' when login() was never given a returnUrl", async () => {
      signinRedirectCallbackMock.mockResolvedValue(fakeUser({ state: undefined }))

      const store = useAuthStore()
      const returnUrl = await store.completeLogin()

      expect(returnUrl).toBe('/')
    })
  })

  describe('logout', () => {
    it('redirects to end the session at the provider', async () => {
      const store = useAuthStore()
      await store.logout()

      expect(signoutRedirectMock).toHaveBeenCalled()
    })
  })
})
