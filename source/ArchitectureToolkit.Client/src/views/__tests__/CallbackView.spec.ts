import { beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import type { User } from 'oidc-client-ts'
import { testVuetify } from '@/test-utils/vuetify'

const signinRedirectCallbackMock = vi.fn()
const pushMock = vi.fn()

vi.mock('@/auth/oidcConfig', () => ({
  userManager: {
    getUser: vi.fn().mockResolvedValue(null),
    signinRedirectCallback: signinRedirectCallbackMock,
    events: { addUserLoaded: vi.fn(), addUserUnloaded: vi.fn() },
  },
}))
vi.mock('vue-router', () => ({ useRouter: () => ({ push: pushMock }) }))

const { default: CallbackView } = await import('../CallbackView.vue')

function mountView() {
  setActivePinia(createPinia())
  return mount(CallbackView, { global: { plugins: [testVuetify] } })
}

describe('CallbackView', () => {
  beforeEach(() => {
    signinRedirectCallbackMock.mockReset()
    pushMock.mockReset()
  })

  it("pushes to the return URL from completeLogin's result on success", async () => {
    signinRedirectCallbackMock.mockResolvedValue({
      state: { returnUrl: '/templates/abc' },
    } as User)

    mountView()
    await flushPromises()

    expect(pushMock).toHaveBeenCalledWith('/templates/abc')
  })

  it('shows an error instead of redirecting when completeLogin fails', async () => {
    signinRedirectCallbackMock.mockRejectedValue(new Error('invalid_grant'))

    const wrapper = mountView()
    await flushPromises()

    expect(pushMock).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('invalid_grant')
  })
})
