import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { testVuetify } from '@/test-utils/vuetify'
import type { CategoryDto, TemplateSummaryDto, UserDto } from '@/api/types'

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: pushMock }),
}))
const pushMock = vi.fn()

const listTemplatesMock = vi.fn()
const createTemplateMock = vi.fn()
vi.mock('@/api/templates', () => ({
  templatesApi: { list: listTemplatesMock, create: createTemplateMock },
}))

const listCategoriesMock = vi.fn()
vi.mock('@/api/categories', () => ({ categoriesApi: { list: listCategoriesMock } }))

const meMock = vi.fn()
vi.mock('@/api/users', () => ({ usersApi: { me: meMock } }))

const { default: TemplateListView } = await import('../TemplateListView.vue')

function categories(): CategoryDto[] {
  // Deliberately out of alphabetical order — category grouping should
  // sort by name, not by the API's own ordering.
  return [
    { id: 'category-z', code: 'ZZZ', name: 'Zeta Category' },
    { id: 'category-a', code: 'AAA', name: 'Alpha Category' },
  ]
}

function templates(): TemplateSummaryDto[] {
  // Within Alpha Category, deliberately out of alphabetical order too.
  return [
    { id: 'template-zebra', categoryId: 'category-a', name: 'Zebra Template', currentVersion: '1.0.0' },
    { id: 'template-mid', categoryId: 'category-z', name: 'Middle Template', currentVersion: '1.0.0' },
    { id: 'template-apple', categoryId: 'category-a', name: 'Apple Template', currentVersion: '1.0.0' },
  ]
}

function contributor(): UserDto {
  return { id: 'user-1', name: 'Cara Contributor', email: 'cara@example.com', systemRole: 'Contributor' }
}

let mountedWrappers: VueWrapper[] = []

async function mountView() {
  setActivePinia(createPinia())
  const wrapper = mount(TemplateListView, {
    attachTo: document.body,
    global: { plugins: [testVuetify] },
  })
  mountedWrappers.push(wrapper)
  await flushPromises()
  return wrapper
}

describe('TemplateListView', () => {
  beforeEach(() => {
    mountedWrappers = []
    pushMock.mockReset()
    listTemplatesMock.mockReset().mockResolvedValue(templates())
    listCategoriesMock.mockReset().mockResolvedValue(categories())
    createTemplateMock.mockReset()
    meMock.mockReset().mockResolvedValue(contributor())
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
    document.body.innerHTML = ''
  })

  it('groups by category alphabetically, and sorts templates within each category alphabetically', async () => {
    const wrapper = await mountView()

    const categoryHeadings = wrapper.findAll('h2').map((h) => h.text())
    expect(categoryHeadings).toEqual(['Alpha Category', 'Zeta Category'])

    const templateTitles = wrapper.findAll('.v-list-item-title').map((t) => t.text())
    expect(templateTitles).toEqual(['Apple Template', 'Zebra Template', 'Middle Template'])
  })

  it('navigates to the clicked template', async () => {
    const wrapper = await mountView()

    const items = wrapper.findAll('.v-list-item')
    const apple = items.find((i) => i.text().includes('Apple Template'))
    await apple!.trigger('click')

    expect(pushMock).toHaveBeenCalledWith('/templates/template-apple')
  })
})
