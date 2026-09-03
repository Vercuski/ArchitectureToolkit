import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, DOMWrapper, type VueWrapper } from '@vue/test-utils'
import { testVuetify } from '@/test-utils/vuetify'
import type { ProjectDocumentDetailDto } from '@/api/types'

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: 'document-1' } }),
  useRouter: () => ({ push: pushMock }),
}))
const pushMock = vi.fn()

const getMock = vi.fn()
const createRevisionMock = vi.fn()
vi.mock('@/api/documents', () => ({
  documentsApi: { get: getMock, createRevision: createRevisionMock },
}))

// The real ApiError, not a mock — the 409 test needs `instanceof ApiError`
// to actually hold.
const { ApiError } = await import('@/api/httpClient')

// @toast-ui/editor's own internals aren't this component's code to
// verify — mocked the same way as CreateDocumentView.spec.ts /
// ReviseTemplateView.spec.ts. A real class, not an arrow-function mock
// implementation, since the component calls `new Editor(...)`.
const setMarkdownMock = vi.fn()
const getMarkdownMock = vi.fn().mockReturnValue('')
const destroyMock = vi.fn()
class MockEditor {
  constructor(options: { initialValue?: string }) {
    // Mirrors the real Editor: getMarkdown() reflects whatever content it
    // was constructed with until setMarkdown changes it.
    getMarkdownMock.mockReturnValue(options.initialValue ?? '')
  }
  setMarkdown = setMarkdownMock
  getMarkdown = getMarkdownMock
  destroy = destroyMock
  // useToastUiFileAttachments (the "attach file" toolbar button) calls
  // these unconditionally at mount time / on upload — not this
  // component's own behavior to verify, same reasoning as the rest of
  // this mock, but they still need to exist so mounting doesn't throw.
  insertToolbarItem = vi.fn()
  insertText = vi.fn()
}
vi.mock('@toast-ui/editor', () => ({ default: MockEditor }))

const { default: ReviseDocumentView } = await import('../ReviseDocumentView.vue')

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

let mountedWrappers: VueWrapper[] = []

async function mountView() {
  const wrapper = mount(ReviseDocumentView, {
    attachTo: document.body,
    global: { plugins: [testVuetify] },
  })
  mountedWrappers.push(wrapper)
  await flushPromises()
  return wrapper
}

describe('ReviseDocumentView', () => {
  beforeEach(() => {
    mountedWrappers = []
    pushMock.mockReset()
    getMock.mockReset().mockResolvedValue(docDetail())
    createRevisionMock.mockReset()
    setMarkdownMock.mockReset()
    getMarkdownMock.mockReset().mockReturnValue('')
    destroyMock.mockReset()
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
    document.body.innerHTML = ''
  })

  it('loads the document and creates the editor with its current content', async () => {
    await mountView()

    expect(getMock).toHaveBeenCalledWith('document-1')
    expect(getMarkdownMock()).toBe('# Data Model ADR\n\nOriginal content.')
  })

  it('saves a revision with the selected bump type and edited content, then navigates back', async () => {
    createRevisionMock.mockResolvedValue({ id: 'revision-2' })

    await mountView()
    // Set after mounting — the MockEditor constructor resets
    // getMarkdownMock's return value to the document's original content
    // when it's created during onMounted.
    getMarkdownMock.mockReturnValue('# Data Model ADR\n\nEdited content.')

    await new DOMWrapper(document.body.querySelector('#confirm-revise-document')!).trigger('click')
    await flushPromises()

    expect(createRevisionMock).toHaveBeenCalledWith(
      'document-1',
      'revision-1',
      'Minor',
      '# Data Model ADR\n\nEdited content.',
    )
    expect(pushMock).toHaveBeenCalledWith('/documents/document-1')
  })

  it('on a 409 conflict, refreshes the stale revision id and lets the user retry without losing their edit', async () => {
    createRevisionMock.mockRejectedValueOnce(new ApiError(409, { error: 'Revision conflict' }))
    createRevisionMock.mockResolvedValueOnce({ id: 'revision-2' })

    await mountView()
    getMarkdownMock.mockReturnValue('# Data Model ADR\n\nMy edit.')
    getMock.mockResolvedValue(docDetail({ currentRevisionId: 'revision-2' }))

    await new DOMWrapper(document.body.querySelector('#confirm-revise-document')!).trigger('click')
    await flushPromises()

    expect(pushMock).not.toHaveBeenCalled()
    expect(document.body.textContent).toContain('Someone else saved a new revision')
    // The reload after a conflict must not stomp on the user's own edit.
    expect(setMarkdownMock).not.toHaveBeenCalled()

    await new DOMWrapper(document.body.querySelector('#confirm-revise-document')!).trigger('click')
    await flushPromises()

    expect(createRevisionMock).toHaveBeenLastCalledWith(
      'document-1',
      'revision-2',
      'Minor',
      '# Data Model ADR\n\nMy edit.',
    )
    expect(pushMock).toHaveBeenCalledWith('/documents/document-1')
  })

  describe('cancel', () => {
    it('navigates away directly when the editor content is unchanged', async () => {
      await mountView()
      await new DOMWrapper(document.body.querySelector('#cancel-revise-document')!).trigger('click')

      expect(pushMock).toHaveBeenCalledWith('/documents/document-1')
      expect(document.body.textContent).not.toContain('Discard changes?')
    })

    it('prompts before discarding when the editor content has changed', async () => {
      await mountView()
      getMarkdownMock.mockReturnValue('# Data Model ADR\n\nSomething I typed.')

      await new DOMWrapper(document.body.querySelector('#cancel-revise-document')!).trigger('click')
      await flushPromises()

      expect(pushMock).not.toHaveBeenCalled()
      expect(document.body.textContent).toContain('Discard changes?')
    })

    it('navigates away once the user confirms discarding', async () => {
      await mountView()
      getMarkdownMock.mockReturnValue('# Data Model ADR\n\nSomething I typed.')

      await new DOMWrapper(document.body.querySelector('#cancel-revise-document')!).trigger('click')
      await flushPromises()
      await new DOMWrapper(document.body.querySelector('#confirm-discard-changes')!).trigger('click')

      expect(pushMock).toHaveBeenCalledWith('/documents/document-1')
    })
  })
})
