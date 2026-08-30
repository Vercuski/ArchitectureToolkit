import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils'
import { testVuetify } from '@/test-utils/vuetify'
import type { CategoryDto, TemplateDetailDto, TemplateSummaryDto } from '@/api/types'

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { projectId: 'project-1' } }),
  useRouter: () => ({ push: pushMock }),
}))
const pushMock = vi.fn()

const listCategoriesMock = vi.fn()
vi.mock('@/api/categories', () => ({ categoriesApi: { list: listCategoriesMock } }))

const listTemplatesMock = vi.fn()
const getTemplateMock = vi.fn()
vi.mock('@/api/templates', () => ({
  templatesApi: { list: listTemplatesMock, get: getTemplateMock },
}))

const createDocumentMock = vi.fn()
vi.mock('@/api/documents', () => ({ documentsApi: { create: createDocumentMock } }))

// @toast-ui/editor's own internals (CodeMirror, its markdown parser, etc.)
// aren't this component's code to verify — mocked here the same way
// Vuetify's own internals are trusted rather than re-tested elsewhere in
// this suite. What's under test is this component's own wiring: does it
// call setMarkdown with the right content, getMarkdown for submission.
// A real class, not an arrow-function mock implementation — arrow
// functions can't be used as constructors, and the component calls
// `new Editor(...)`.
const setMarkdownMock = vi.fn()
const getMarkdownMock = vi.fn().mockReturnValue('')
const destroyMock = vi.fn()
class MockEditor {
  setMarkdown = setMarkdownMock
  getMarkdown = getMarkdownMock
  destroy = destroyMock
}
vi.mock('@toast-ui/editor', () => ({ default: MockEditor }))

const { default: CreateDocumentView } = await import('../CreateDocumentView.vue')

function categories(): CategoryDto[] {
  return [
    { id: 'category-1', code: 'ADR', name: 'Architecture Decisions' },
    { id: 'category-2', code: 'REQ', name: 'Requirements' },
  ]
}

function templates(): TemplateSummaryDto[] {
  return [
    { id: 'template-1', categoryId: 'category-1', name: 'ADR Template', currentVersion: '1.0.0' },
    { id: 'template-2', categoryId: 'category-2', name: 'Requirements Template', currentVersion: '1.0.0' },
  ]
}

function templateDetail(overrides: Partial<TemplateDetailDto> = {}): TemplateDetailDto {
  return {
    id: 'template-1',
    categoryId: 'category-1',
    name: 'ADR Template',
    currentVersion: '1.0.0',
    currentRevisionId: 'template-revision-1',
    content: '# ADR Template\n\nStarting content.',
    ...overrides,
  }
}

let mountedWrappers: VueWrapper[] = []

async function mountView() {
  const wrapper = mount(CreateDocumentView, {
    attachTo: document.body,
    global: { plugins: [testVuetify] },
  })
  mountedWrappers.push(wrapper)
  await flushPromises()
  return wrapper
}

describe('CreateDocumentView', () => {
  beforeEach(() => {
    mountedWrappers = []
    pushMock.mockReset()
    listCategoriesMock.mockReset().mockResolvedValue(categories())
    listTemplatesMock.mockReset().mockResolvedValue(templates())
    getTemplateMock.mockReset().mockResolvedValue(templateDetail())
    createDocumentMock.mockReset()
    setMarkdownMock.mockReset()
    getMarkdownMock.mockReset().mockReturnValue('')
    destroyMock.mockReset()
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
    document.body.innerHTML = ''
  })

  it('loads categories and templates on mount', async () => {
    await mountView()

    expect(listCategoriesMock).toHaveBeenCalled()
    expect(listTemplatesMock).toHaveBeenCalled()
  })

  it("prefills the editor and category from the selected template's detail", async () => {
    const wrapper = await mountView()
    const vm = wrapper.vm as unknown as { onTemplateSelected: (id: string | null) => Promise<void> }
    await vm.onTemplateSelected('template-1')
    await flushPromises()

    expect(getTemplateMock).toHaveBeenCalledWith('template-1')
    expect(setMarkdownMock).toHaveBeenCalledWith('# ADR Template\n\nStarting content.')
  })

  it('creates the document using the cached template detail and the editor content, then navigates to it', async () => {
    createDocumentMock.mockResolvedValue({ id: 'document-1' })
    getMarkdownMock.mockReturnValue('# ADR Template\n\nEdited content.')

    const wrapper = await mountView()
    const vm = wrapper.vm as unknown as {
      onTemplateSelected: (id: string | null) => Promise<void>
      title: string
      createDocument: () => Promise<void>
    }
    await vm.onTemplateSelected('template-1')
    await flushPromises()
    vm.title = 'My New Document'
    await wrapper.vm.$nextTick()
    await vm.createDocument()
    await flushPromises()

    // getTemplateMock only called once (from onTemplateSelected) — the
    // cached currentRevisionId is reused, not refetched on submit.
    expect(getTemplateMock).toHaveBeenCalledTimes(1)
    expect(createDocumentMock).toHaveBeenCalledWith(
      'project-1',
      'category-1',
      'My New Document',
      '# ADR Template\n\nEdited content.',
      'template-revision-1',
    )
    expect(pushMock).toHaveBeenCalledWith('/documents/document-1')
  })

  it('destroys the editor instance on unmount', async () => {
    const wrapper = await mountView()
    wrapper.unmount()
    mountedWrappers = []

    expect(destroyMock).toHaveBeenCalled()
  })
})
