import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { testVuetify } from '@/test-utils/vuetify'
import type { TemplateDetailDto, TemplateRevisionDto, UserDto } from '@/api/types'

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: 'template-1' } }),
}))

const getMock = vi.fn()
const listRevisionsMock = vi.fn()
const getRevisionMock = vi.fn()
vi.mock('@/api/templates', () => ({
  templatesApi: {
    get: getMock,
    listRevisions: listRevisionsMock,
    getRevision: getRevisionMock,
  },
}))

const meMock = vi.fn()
vi.mock('@/api/users', () => ({ usersApi: { me: meMock } }))

const { default: TemplateDetailView } = await import('../TemplateDetailView.vue')

function architect(): UserDto {
  return { id: 'user-1', name: 'Ana Architect', email: 'ana@example.com', systemRole: 'Architect' }
}

function contributor(): UserDto {
  return { id: 'user-2', name: 'Cara Contributor', email: 'cara@example.com', systemRole: 'Contributor' }
}

function template(overrides: Partial<TemplateDetailDto> = {}): TemplateDetailDto {
  return {
    id: 'template-1',
    categoryId: 'category-1',
    name: 'ADR Template',
    currentVersion: '1.0.0',
    currentRevisionId: 'revision-1',
    content: '# ADR Template\n\nOriginal content.',
    ...overrides,
  }
}

function revisions(): TemplateRevisionDto[] {
  return [
    {
      id: 'revision-1',
      templateId: 'template-1',
      version: '1.0.0',
      bumpType: null,
      authorId: 'user-1',
      createdAt: '2026-01-01T00:00:00Z',
    },
  ]
}

async function mountView() {
  setActivePinia(createPinia())
  const wrapper = mount(TemplateDetailView, {
    attachTo: document.body,
    global: { plugins: [testVuetify] },
  })
  mountedWrappers.push(wrapper)
  await flushPromises()
  return wrapper
}

// v-dialog content is teleported to document.body rather than nested
// under the component's own root, so a test that throws before reaching
// its own wrapper.unmount() would otherwise leave that markup behind for
// the next test to trip over. Tracking every mount here and tearing all
// of them down in afterEach — regardless of how the test ended — keeps
// each test's document.body genuinely empty at the start.
let mountedWrappers: VueWrapper[] = []

describe('TemplateDetailView', () => {
  beforeEach(() => {
    mountedWrappers = []
    getMock.mockReset().mockResolvedValue(template())
    listRevisionsMock.mockReset().mockResolvedValue(revisions())
    getRevisionMock.mockReset()
    meMock.mockReset().mockResolvedValue(architect())
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
    document.body.innerHTML = ''
  })

  it('loads and displays the template content and revision history', async () => {
    const wrapper = await mountView()

    expect(getMock).toHaveBeenCalledWith('template-1')
    expect(wrapper.text()).toContain('ADR Template')
    expect(wrapper.text()).toContain('v1.0.0')
    expect(wrapper.text()).toContain('Original content')
    expect(wrapper.find('tbody tr').exists()).toBe(true)
  })

  it('shows "New Revision" to an Architect but not to a Contributor', async () => {
    const architectWrapper = await mountView()
    expect(architectWrapper.find('#new-revision-button').exists()).toBe(true)

    meMock.mockResolvedValue(contributor())
    const contributorWrapper = await mountView()
    expect(contributorWrapper.find('#new-revision-button').exists()).toBe(false)
  })

  it('links "New Revision" to the dedicated revise page', async () => {
    const wrapper = await mountView()
    // findComponent(cssSelector) types as WrapperLike (it could match a
    // plain element), but this selector always matches the VBtn itself —
    // cast to access .props(). The unparameterized VueWrapper type also
    // can't statically know VBtn's prop names, hence the second cast.
    const button = wrapper.findComponent('#new-revision-button') as VueWrapper
    const props = button.props() as Record<string, unknown>

    expect(props.to).toBe('/templates/template-1/revise')
  })

  it('opens a historical revision on row click', async () => {
    getRevisionMock.mockResolvedValue({
      ...revisions()[0],
      content: '# ADR Template\n\nOriginal content.',
    })

    const wrapper = await mountView()
    await wrapper.find('tbody tr').trigger('click')
    await flushPromises()

    expect(getRevisionMock).toHaveBeenCalledWith('template-1', 'revision-1')
    expect(document.body.textContent).toContain('Version 1.0.0')
  })
})
