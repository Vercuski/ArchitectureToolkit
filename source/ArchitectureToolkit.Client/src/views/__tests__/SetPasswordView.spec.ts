import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils'
import { testVuetify } from '@/test-utils/vuetify'
import { ApiError } from '@/api/httpClient'

const setPasswordMock = vi.fn()
vi.mock('@/api/account', () => ({ accountApi: { setPassword: setPasswordMock } }))

const loginMock = vi.fn()
vi.mock('@/stores/auth', () => ({ useAuthStore: () => ({ login: loginMock }) }))

let routeQuery: Record<string, string> = { email: 'new.hire@example.com', token: 'token-abc' }
vi.mock('vue-router', () => ({ useRoute: () => ({ query: routeQuery }) }))

const { default: SetPasswordView } = await import('../SetPasswordView.vue')

let mountedWrappers: VueWrapper[] = []

function mountView() {
  const wrapper = mount(SetPasswordView, { global: { plugins: [testVuetify] } })
  mountedWrappers.push(wrapper)
  return wrapper
}

describe('SetPasswordView', () => {
  beforeEach(() => {
    mountedWrappers = []
    routeQuery = { email: 'new.hire@example.com', token: 'token-abc' }
    setPasswordMock.mockReset()
    loginMock.mockReset()
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
  })

  it('shows an error and no form when the link is missing email or token', () => {
    routeQuery = {}
    const wrapper = mountView()

    expect(wrapper.text()).toContain('missing required information')
    expect(wrapper.find('#set-password-submit').exists()).toBe(false)
  })

  it('shows an error without calling the API when passwords do not match', async () => {
    const wrapper = mountView()

    await wrapper.find('#set-password-new').setValue('Password123!')
    await wrapper.find('#set-password-confirm').setValue('Different123!')
    await wrapper.find('#set-password-submit').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Passwords do not match.')
    expect(setPasswordMock).not.toHaveBeenCalled()
  })

  it('submits the email/token from the query string with the new password', async () => {
    setPasswordMock.mockResolvedValue(undefined)
    const wrapper = mountView()

    await wrapper.find('#set-password-new').setValue('Password123!')
    await wrapper.find('#set-password-confirm').setValue('Password123!')
    await wrapper.find('#set-password-submit').trigger('click')
    await flushPromises()

    expect(setPasswordMock).toHaveBeenCalledWith(
      'new.hire@example.com', 'token-abc', 'Password123!', 'Password123!',
    )
    expect(wrapper.text()).toContain('Your password has been set.')
  })

  it('shows an API error (e.g. expired token) instead of the success state', async () => {
    setPasswordMock.mockRejectedValue(new ApiError(400, { error: 'This link is invalid or has expired.' }))
    const wrapper = mountView()

    await wrapper.find('#set-password-new').setValue('Password123!')
    await wrapper.find('#set-password-confirm').setValue('Password123!')
    await wrapper.find('#set-password-submit').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('This link is invalid or has expired.')
    expect(wrapper.text()).not.toContain('Your password has been set.')
  })

  it('triggers sign-in after success, which is what completes the identity link (ADR-0018)', async () => {
    setPasswordMock.mockResolvedValue(undefined)
    const wrapper = mountView()

    await wrapper.find('#set-password-new').setValue('Password123!')
    await wrapper.find('#set-password-confirm').setValue('Password123!')
    await wrapper.find('#set-password-submit').trigger('click')
    await flushPromises()
    await wrapper.find('#sign-in-after-set-password').trigger('click')

    expect(loginMock).toHaveBeenCalled()
  })
})
