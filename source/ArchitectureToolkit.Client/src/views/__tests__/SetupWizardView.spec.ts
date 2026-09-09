import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { testVuetify } from '@/test-utils/vuetify'

const pushMock = vi.fn()
const replaceMock = vi.fn()
vi.mock('vue-router', () => ({ useRouter: () => ({ push: pushMock, replace: replaceMock }) }))

const completeMock = vi.fn()
const statusMock = vi.fn()
vi.mock('@/api/setup', () => ({ setupApi: { complete: completeMock, status: statusMock } }))

const { default: SetupWizardView } = await import('../SetupWizardView.vue')

let mountedWrappers: VueWrapper[] = []

async function mountView() {
  const wrapper = mount(SetupWizardView, {
    attachTo: document.body,
    global: { plugins: [testVuetify] },
  })
  mountedWrappers.push(wrapper)
  await flushPromises()
  return wrapper
}

/** Only the two fields the form's defaults don't already pre-fill. */
async function fillRequiredInitialUserFields(wrapper: VueWrapper) {
  await wrapper.find('#setup-initial-user-email').setValue('scott@example.com')
  await wrapper.find('#setup-initial-user-password').setValue('Correct-Horse-Battery-9!')
  await wrapper.find('#setup-initial-user-confirm-password').setValue('Correct-Horse-Battery-9!')
}

describe('SetupWizardView', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    setActivePinia(createPinia())
    mountedWrappers = []
    pushMock.mockReset()
    replaceMock.mockReset().mockResolvedValue(undefined)
    completeMock.mockReset()
    statusMock.mockReset()
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
    document.body.innerHTML = ''
    vi.useRealTimers()
  })

  it('shows the restarting phase after a successful save, then returns home once the API reports configured', async () => {
    completeMock.mockResolvedValue(undefined)
    statusMock.mockResolvedValue({ isConfigured: true })

    const wrapper = await mountView()
    await fillRequiredInitialUserFields(wrapper)
    await wrapper.find('#setup-save').trigger('click')
    await flushPromises()

    expect(completeMock).toHaveBeenCalledOnce()
    expect(wrapper.text()).toContain('Setup complete — restarting')

    await vi.advanceTimersByTimeAsync(3000)
    await flushPromises()

    expect(replaceMock).toHaveBeenCalledWith({ name: 'home' })
  })

  it('shows a reload fallback if the API never comes back within the poll budget', async () => {
    completeMock.mockResolvedValue(undefined)
    statusMock.mockResolvedValue({ isConfigured: false })

    const wrapper = await mountView()
    await fillRequiredInitialUserFields(wrapper)
    await wrapper.find('#setup-save').trigger('click')
    await flushPromises()

    await vi.advanceTimersByTimeAsync(3000 + 60 * 2000)
    await flushPromises()

    expect(replaceMock).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain("hasn't come back up yet")
  })

  it('shows field-level errors from the server without leaving the form', async () => {
    const { ApiError } = await import('@/api/httpClient')
    completeMock.mockRejectedValue(
      new ApiError(400, { errors: [{ field: 'SmtpPort', message: 'SmtpPort must be between 1 and 65535.' }] }),
    )

    const wrapper = await mountView()
    await fillRequiredInitialUserFields(wrapper)
    await wrapper.find('#setup-save').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Please correct the highlighted fields.')
    expect(wrapper.find('#setup-save').exists()).toBe(true)
  })
})
