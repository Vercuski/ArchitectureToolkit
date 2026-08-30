import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { testVuetify } from '@/test-utils/vuetify'
import type { CategoryDto, ProjectDocumentDetailDto, ProjectMemberDto, DocumentRevisionDto } from '@/api/types'

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: 'document-1' } }),
}))

vi.mock('@/auth/oidcConfig', () => ({
  userManager: {
    getUser: vi.fn().mockResolvedValue(null),
    events: { addUserLoaded: vi.fn(), addUserUnloaded: vi.fn() },
  },
}))

const getMock = vi.fn()
const listRevisionsMock = vi.fn()
const getRevisionMock = vi.fn()
vi.mock('@/api/documents', () => ({
  documentsApi: {
    get: getMock,
    listRevisions: listRevisionsMock,
    getRevision: getRevisionMock,
  },
}))

const listMembersMock = vi.fn()
vi.mock('@/api/projects', () => ({ projectsApi: { listMembers: listMembersMock } }))

const listCategoriesMock = vi.fn()
vi.mock('@/api/categories', () => ({ categoriesApi: { list: listCategoriesMock } }))

const { useAuthStore } = await import('@/stores/auth')
const { default: DocumentDetailView } = await import('../DocumentDetailView.vue')

function docDetail(overrides: Partial<ProjectDocumentDetailDto> = {}): ProjectDocumentDetailDto {
  return {
    id: 'document-1',
    projectId: 'project-1',
    categoryId: 'category-1',
    title: 'Data Model ADR',
    currentVersion: '1.0.0',
    currentRevisionId: 'revision-1',
    sourceTemplateRevisionId: null,
    content: '# Data Model ADR\n\nOriginal content.',
    ...overrides,
  }
}

function categories(): CategoryDto[] {
  return [{ id: 'category-1', code: 'ADR', name: 'Architecture Decisions' }]
}

function member(role: string): ProjectMemberDto {
  return {
    projectId: 'project-1',
    userId: 'user-1',
    userName: 'Ana',
    userEmail: 'ana@example.com',
    role: role as ProjectMemberDto['role'],
  }
}

function revisions(): DocumentRevisionDto[] {
  return [
    {
      id: 'revision-1',
      documentId: 'document-1',
      version: '1.0.0',
      bumpType: null,
      authorId: 'user-1',
      createdAt: '2026-01-01T00:00:00Z',
    },
  ]
}

let mountedWrappers: VueWrapper[] = []

async function mountView() {
  const pinia = createPinia()
  setActivePinia(pinia)
  const authStore = useAuthStore()
  authStore.user = { profile: { email: 'ana@example.com' } } as never

  const wrapper = mount(DocumentDetailView, {
    attachTo: document.body,
    global: { plugins: [testVuetify, pinia] },
  })
  mountedWrappers.push(wrapper)
  await flushPromises()
  return wrapper
}

describe('DocumentDetailView', () => {
  beforeEach(() => {
    mountedWrappers = []
    getMock.mockReset().mockResolvedValue(docDetail())
    listRevisionsMock.mockReset().mockResolvedValue(revisions())
    listCategoriesMock.mockReset().mockResolvedValue(categories())
    listMembersMock.mockReset().mockResolvedValue([member('Editor')])
    getRevisionMock.mockReset()
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
    document.body.innerHTML = ''
  })

  it('loads and displays the document content, category, version, and history', async () => {
    const wrapper = await mountView()

    expect(getMock).toHaveBeenCalledWith('document-1')
    expect(listMembersMock).toHaveBeenCalledWith('project-1')
    expect(wrapper.text()).toContain('Data Model ADR')
    expect(wrapper.text()).toContain('Architecture Decisions')
    expect(wrapper.text()).toContain('v1.0.0')
    expect(wrapper.find('tbody tr').exists()).toBe(true)
  })

  it('shows the "started from a template" chip only when sourceTemplateRevisionId is set', async () => {
    const wrapper = await mountView()
    expect(wrapper.text()).not.toContain('Started from a template')

    getMock.mockResolvedValue(docDetail({ sourceTemplateRevisionId: 'template-revision-1' }))
    const fromTemplateWrapper = await mountView()
    expect(fromTemplateWrapper.text()).toContain('Started from a template')
  })

  it('shows "New Revision" to an Editor or Owner but not a Viewer', async () => {
    const editorWrapper = await mountView()
    expect(editorWrapper.find('#new-document-revision-button').exists()).toBe(true)

    listMembersMock.mockResolvedValue([member('Viewer')])
    const viewerWrapper = await mountView()
    expect(viewerWrapper.find('#new-document-revision-button').exists()).toBe(false)
  })

  it('links "New Revision" to the dedicated revise page', async () => {
    const wrapper = await mountView()
    // findComponent(cssSelector) types as WrapperLike (it could match a
    // plain element), but this selector always matches the VBtn itself —
    // cast to access .props(). The unparameterized VueWrapper type also
    // can't statically know VBtn's prop names, hence the second cast.
    const button = wrapper.findComponent('#new-document-revision-button') as VueWrapper
    const props = button.props() as Record<string, unknown>

    expect(props.to).toBe('/documents/document-1/revise')
  })
})
