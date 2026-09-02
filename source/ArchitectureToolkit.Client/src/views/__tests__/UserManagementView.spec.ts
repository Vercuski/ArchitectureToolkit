import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, DOMWrapper, type VueWrapper } from '@vue/test-utils'
import { testVuetify } from '@/test-utils/vuetify'
import { ApiError } from '@/api/httpClient'
import type { UserManagementDto } from '@/api/types'

const listMock = vi.fn()
const setActiveMock = vi.fn()
const createMock = vi.fn()
vi.mock('@/api/users', () => ({
  usersApi: { list: listMock, setActive: setActiveMock, create: createMock },
}))

const getConfigMock = vi.fn()
vi.mock('@/api/auth', () => ({ authApi: { getConfig: getConfigMock } }))

const { default: UserManagementView } = await import('../UserManagementView.vue')

function users(): UserManagementDto[] {
  // Server is the source of truth for ordering (ListUsersQueryHandler
  // sorts by email) — the view should render this order as-is, not
  // re-sort it.
  return [
    { id: 'user-bea', email: 'bea@example.com', isActive: true },
    { id: 'user-mia', email: 'mia@example.com', isActive: false },
    { id: 'user-zed', email: 'zed@example.com', isActive: true },
  ]
}

let mountedWrappers: VueWrapper[] = []

// v-dialog teleports its content to document.body, outside the mounted
// wrapper's own element tree — wrapper.find() can't see it, so anything
// inside the dialog goes through this instead.
function body() {
  return new DOMWrapper(document.body)
}

async function mountView() {
  const wrapper = mount(UserManagementView, {
    attachTo: document.body,
    global: { plugins: [testVuetify] },
  })
  mountedWrappers.push(wrapper)
  await flushPromises()
  return wrapper
}

describe('UserManagementView', () => {
  beforeEach(() => {
    mountedWrappers = []
    listMock.mockReset().mockResolvedValue(users())
    setActiveMock.mockReset()
    createMock.mockReset()
    getConfigMock.mockReset().mockResolvedValue({ useSelfHostedProvider: true })
    Object.assign(navigator, { clipboard: { writeText: vi.fn().mockResolvedValue(undefined) } })
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
    document.body.innerHTML = ''
  })

  it('renders every user in the order the API returns', async () => {
    const wrapper = await mountView()

    const rows = wrapper.findAll('tbody tr')
    expect(rows).toHaveLength(3)
    expect(rows.map((r) => r.text())).toEqual([
      expect.stringContaining('bea@example.com'),
      expect.stringContaining('mia@example.com'),
      expect.stringContaining('zed@example.com'),
    ])
  })

  it("shows each user's active status via the switch", async () => {
    const wrapper = await mountView()

    const switches = wrapper.findAllComponents({ name: 'VSwitch' })
    expect(switches).toHaveLength(3)
    // bea (active), mia (inactive), zed (active) — same order as rendered.
    expect(switches.map((s) => s.props('modelValue'))).toEqual([true, false, true])
  })

  it('copies the user id to the clipboard when the copy button is clicked', async () => {
    const wrapper = await mountView()

    const copyButtons = wrapper.findAll('button[title="Copy User ID"]')
    await copyButtons[0]!.trigger('click')

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith('user-bea')
  })

  it('deactivates a user when their switch is toggled off', async () => {
    setActiveMock.mockResolvedValue({ id: 'user-bea', email: 'bea@example.com', isActive: false })
    const wrapper = await mountView()

    const switches = wrapper.findAllComponents({ name: 'VSwitch' })
    await switches[0]!.vm.$emit('update:modelValue', false)
    await flushPromises()

    expect(setActiveMock).toHaveBeenCalledWith('user-bea', false)
    expect(switches[0]!.props('modelValue')).toBe(false)
  })

  it('reverts the switch and shows an error when the toggle fails', async () => {
    setActiveMock.mockRejectedValue(
      new ApiError(409, { error: 'Cannot deactivate the last remaining active architect.' }),
    )
    const wrapper = await mountView()

    const switches = wrapper.findAllComponents({ name: 'VSwitch' })
    await switches[0]!.vm.$emit('update:modelValue', false)
    await flushPromises()

    expect(switches[0]!.props('modelValue')).toBe(true)
    expect(wrapper.text()).toContain('Cannot deactivate the last remaining active architect.')
  })

  it('shows a load error when the caller is not an architect', async () => {
    listMock.mockReset().mockRejectedValue(
      new ApiError(403, { error: 'Only an architect may view the user list.' }),
    )
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Only an architect may view the user list.')
    expect(wrapper.find('table').exists()).toBe(false)
  })

  it('shows the New User button for a self-hosted deployment', async () => {
    const wrapper = await mountView()

    expect(wrapper.find('#new-user-button').exists()).toBe(true)
  })

  it('hides the New User button for an external-Authority deployment', async () => {
    getConfigMock.mockReset().mockResolvedValue({ useSelfHostedProvider: false })
    const wrapper = await mountView()

    expect(wrapper.find('#new-user-button').exists()).toBe(false)
  })

  it('hides the New User button when the config check itself fails', async () => {
    getConfigMock.mockReset().mockRejectedValue(new ApiError(401, {}))
    const wrapper = await mountView()

    expect(wrapper.find('#new-user-button').exists()).toBe(false)
  })

  it('creates a user, shows the email-sent banner, and refreshes the list', async () => {
    createMock.mockResolvedValue({
      user: { id: 'user-new', email: 'new.hire@example.com', isActive: true },
      emailSent: true,
      inviteLink: null,
    })
    const wrapper = await mountView()

    await wrapper.find('#new-user-button').trigger('click')
    await flushPromises()
    await body().find('#new-user-email').setValue('new.hire@example.com')
    await body().find('#confirm-create-user').trigger('click')
    await flushPromises()

    expect(createMock).toHaveBeenCalledWith('new.hire@example.com', 'Contributor')
    expect(wrapper.text()).toContain('Invite email sent to new.hire@example.com')
    expect(listMock).toHaveBeenCalledTimes(2)
  })

  it('shows a copyable link when the invite email was not sent', async () => {
    createMock.mockResolvedValue({
      user: { id: 'user-new', email: 'new.hire@example.com', isActive: true },
      emailSent: false,
      inviteLink: 'https://app.example.com/set-password?email=new.hire%40example.com&token=abc',
    })
    const wrapper = await mountView()

    await wrapper.find('#new-user-button').trigger('click')
    await flushPromises()
    await body().find('#new-user-email').setValue('new.hire@example.com')
    await body().find('#confirm-create-user').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain("SMTP isn't configured")
    expect(wrapper.text()).toContain('https://app.example.com/set-password?email=new.hire%40example.com&token=abc')
  })

  it('shows a duplicate-email error inside the dialog without closing it', async () => {
    createMock.mockRejectedValue(new ApiError(409, { error: 'A user with this email already exists.' }))
    const wrapper = await mountView()

    await wrapper.find('#new-user-button').trigger('click')
    await flushPromises()
    await body().find('#new-user-email').setValue('duplicate@example.com')
    await body().find('#confirm-create-user').trigger('click')
    await flushPromises()

    expect(body().text()).toContain('A user with this email already exists.')
    expect(listMock).toHaveBeenCalledTimes(1)
  })
})
