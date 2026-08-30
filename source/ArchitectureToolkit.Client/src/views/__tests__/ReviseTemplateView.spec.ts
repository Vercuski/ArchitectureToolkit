import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, DOMWrapper, type VueWrapper } from '@vue/test-utils'
import { testVuetify } from '@/test-utils/vuetify'
import type { TemplateDetailDto } from '@/api/types'

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: 'template-1' } }),
  useRouter: () => ({ push: pushMock }),
}))
const pushMock = vi.fn()

const getMock = vi.fn()
const createRevisionMock = vi.fn()
vi.mock('@/api/templates', () => ({
  templatesApi: { get: getMock, createRevision: createRevisionMock },
}))

// The real ApiError, not a mock — the 409 test needs `instanceof ApiError`
// to actually hold.
const { ApiError } = await import('@/api/httpClient')

// @toast-ui/editor's own internals aren't this component's code to
// verify — mocked the same way as CreateDocumentView.spec.ts. A real
// class, not an arrow-function mock implementation, since the component
// calls `new Editor(...)`.
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
}
vi.mock('@toast-ui/editor', () => ({ default: MockEditor }))

const { default: ReviseTemplateView } = await import('../ReviseTemplateView.vue')

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

let mountedWrappers: VueWrapper[] = []

async function mountView() {
  const wrapper = mount(ReviseTemplateView, {
    attachTo: document.body,
    global: { plugins: [testVuetify] },
  })
  mountedWrappers.push(wrapper)
  await flushPromises()
  return wrapper
}

describe('ReviseTemplateView', () => {
  beforeEach(() => {
    mountedWrappers = []
    pushMock.mockReset()
    getMock.mockReset().mockResolvedValue(template())
    createRevisionMock.mockReset()
    setMarkdownMock.mockReset()
    getMarkdownMock.mockReset().mockReturnValue('')
    destroyMock.mockReset()
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
    document.body.innerHTML = ''
  })

  it("loads the template and creates the editor with its current content", async () => {
    await mountView()

    expect(getMock).toHaveBeenCalledWith('template-1')
    expect(getMarkdownMock()).toBe('# ADR Template\n\nOriginal content.')
  })

  it('saves a revision with the selected bump type and edited content, then navigates back', async () => {
    createRevisionMock.mockResolvedValue({ id: 'revision-2' })

    await mountView()
    // Set after mounting — the MockEditor constructor resets
    // getMarkdownMock's return value to the template's original content
    // when it's created during onMounted.
    getMarkdownMock.mockReturnValue('# ADR Template\n\nEdited content.')

    await new DOMWrapper(document.body.querySelector('#confirm-revise-template')!).trigger('click')
    await flushPromises()

    expect(createRevisionMock).toHaveBeenCalledWith(
      'template-1',
      'revision-1',
      'Minor',
      '# ADR Template\n\nEdited content.',
    )
    expect(pushMock).toHaveBeenCalledWith('/templates/template-1')
  })

  it('on a 409 conflict, refreshes the stale revision id and lets the user retry without losing their edit', async () => {
    createRevisionMock.mockRejectedValueOnce(new ApiError(409, { error: 'Revision conflict' }))
    createRevisionMock.mockResolvedValueOnce({ id: 'revision-2' })

    await mountView()
    getMarkdownMock.mockReturnValue('# ADR Template\n\nMy edit.')
    getMock.mockResolvedValue(template({ currentRevisionId: 'revision-2' }))

    await new DOMWrapper(document.body.querySelector('#confirm-revise-template')!).trigger('click')
    await flushPromises()

    expect(pushMock).not.toHaveBeenCalled()
    expect(document.body.textContent).toContain('Someone else saved a new revision')
    // The reload after a conflict must not stomp on the user's own edit.
    expect(setMarkdownMock).not.toHaveBeenCalled()

    await new DOMWrapper(document.body.querySelector('#confirm-revise-template')!).trigger('click')
    await flushPromises()

    expect(createRevisionMock).toHaveBeenLastCalledWith(
      'template-1',
      'revision-2',
      'Minor',
      '# ADR Template\n\nMy edit.',
    )
    expect(pushMock).toHaveBeenCalledWith('/templates/template-1')
  })

  describe('cancel', () => {
    it('navigates away directly when the editor content is unchanged', async () => {
      await mountView()
      await new DOMWrapper(document.body.querySelector('#cancel-revise-template')!).trigger('click')

      expect(pushMock).toHaveBeenCalledWith('/templates/template-1')
      expect(document.body.textContent).not.toContain('Discard changes?')
    })

    it('prompts before discarding when the editor content has changed', async () => {
      await mountView()
      getMarkdownMock.mockReturnValue('# ADR Template\n\nSomething I typed.')

      await new DOMWrapper(document.body.querySelector('#cancel-revise-template')!).trigger('click')
      await flushPromises()

      expect(pushMock).not.toHaveBeenCalled()
      expect(document.body.textContent).toContain('Discard changes?')
    })

    it('navigates away once the user confirms discarding', async () => {
      await mountView()
      getMarkdownMock.mockReturnValue('# ADR Template\n\nSomething I typed.')

      await new DOMWrapper(document.body.querySelector('#cancel-revise-template')!).trigger('click')
      await flushPromises()
      await new DOMWrapper(document.body.querySelector('#confirm-discard-changes')!).trigger('click')

      expect(pushMock).toHaveBeenCalledWith('/templates/template-1')
    })
  })
})
