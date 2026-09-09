import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils'
import { testVuetify } from '@/test-utils/vuetify'
import { ApiError } from '@/api/httpClient'
import type { SettingsDto } from '@/api/types'

const getMock = vi.fn()
const updateMock = vi.fn()
vi.mock('@/api/settings', () => ({ settingsApi: { get: getMock, update: updateMock } }))

const statusMock = vi.fn()
vi.mock('@/api/setup', () => ({ setupApi: { status: statusMock } }))

const { default: SettingsView } = await import('../SettingsView.vue')

function currentSettings(overrides: Partial<SettingsDto> = {}): SettingsDto {
  return {
    templateLibraryRootPath: '/app/DocumentationTemplates',
    smtpHost: 'smtp.example.com',
    smtpPort: 587,
    smtpUsername: 'notifications@example.com',
    smtpPasswordConfigured: true,
    smtpFromAddress: 'no-reply@example.com',
    smtpFromName: 'ArchitectureToolkit',
    smtpUseSslOnConnect: false,
    ...overrides,
  }
}

let mountedWrappers: VueWrapper[] = []

async function mountView() {
  const wrapper = mount(SettingsView, {
    attachTo: document.body,
    global: { plugins: [testVuetify] },
  })
  mountedWrappers.push(wrapper)
  await flushPromises()
  return wrapper
}

describe('SettingsView', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    mountedWrappers = []
    getMock.mockReset()
    updateMock.mockReset()
    statusMock.mockReset()
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
    document.body.innerHTML = ''
    vi.useRealTimers()
  })

  it('pre-fills the form from the current settings, without ever showing a password', async () => {
    getMock.mockResolvedValue(currentSettings())

    const wrapper = await mountView()

    expect((wrapper.find('#settings-template-library-root-path').element as HTMLInputElement).value).toBe(
      '/app/DocumentationTemplates',
    )
    expect((wrapper.find('#settings-smtp-host').element as HTMLInputElement).value).toBe('smtp.example.com')
    expect((wrapper.find('#settings-smtp-password').element as HTMLInputElement).value).toBe('')
    expect(wrapper.text()).toContain('A password is currently set')
  })

  it('hints that no password is set when none is configured', async () => {
    getMock.mockResolvedValue(currentSettings({ smtpPasswordConfigured: false }))

    const wrapper = await mountView()

    expect(wrapper.text()).toContain('No password currently set')
  })

  it('shows the server error when loading is forbidden (non-architect)', async () => {
    getMock.mockRejectedValue(new ApiError(403, { error: 'Only an architect may view or change settings.' }))

    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Only an architect may view or change settings.')
    expect(wrapper.find('#settings-save').exists()).toBe(false)
  })

  it('omits the password from the save payload when the field is left blank', async () => {
    getMock.mockResolvedValue(currentSettings())
    updateMock.mockResolvedValue(undefined)
    statusMock.mockResolvedValue({ isConfigured: true })

    const wrapper = await mountView()
    await wrapper.find('#settings-smtp-from-name').setValue('New Name')
    await wrapper.find('#settings-save').trigger('click')
    await flushPromises()

    expect(updateMock).toHaveBeenCalledWith(expect.objectContaining({ SmtpPassword: undefined }))
  })

  it('sends a new password when one is typed', async () => {
    getMock.mockResolvedValue(currentSettings())
    updateMock.mockResolvedValue(undefined)
    statusMock.mockResolvedValue({ isConfigured: true })

    const wrapper = await mountView()
    await wrapper.find('#settings-smtp-password').setValue('new-password-123')
    await wrapper.find('#settings-save').trigger('click')
    await flushPromises()

    expect(updateMock).toHaveBeenCalledWith(expect.objectContaining({ SmtpPassword: 'new-password-123' }))
  })

  it('shows the restarting screen, then reloads and confirms success once the API is back up', async () => {
    getMock.mockResolvedValue(currentSettings())
    updateMock.mockResolvedValue(undefined)
    statusMock.mockResolvedValue({ isConfigured: true })

    const wrapper = await mountView()
    await wrapper.find('#settings-save').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Settings saved — restarting')

    await vi.advanceTimersByTimeAsync(3000)
    await flushPromises()

    expect(getMock).toHaveBeenCalledTimes(2) // initial load + reload after restart
    expect(wrapper.text()).toContain('Settings saved.')
  })

  it('shows field errors from the server without losing the entered values', async () => {
    getMock.mockResolvedValue(currentSettings())
    updateMock.mockRejectedValue(
      new ApiError(400, { errors: [{ field: 'SmtpFromAddress', message: 'SMTP from address is required.' }] }),
    )

    const wrapper = await mountView()
    await wrapper.find('#settings-smtp-from-address').setValue('')
    await wrapper.find('#settings-save').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Please correct the highlighted fields.')
    expect(wrapper.find('#settings-save').exists()).toBe(true)
  })
})
