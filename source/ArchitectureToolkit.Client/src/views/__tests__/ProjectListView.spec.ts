import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils'
import { testVuetify } from '@/test-utils/vuetify'
import type { ProjectDto } from '@/api/types'

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: pushMock }),
}))
const pushMock = vi.fn()

const listMock = vi.fn()
const createMock = vi.fn()
vi.mock('@/api/projects', () => ({
  projectsApi: { list: listMock, create: createMock },
}))

const { default: ProjectListView } = await import('../ProjectListView.vue')

function projects(): ProjectDto[] {
  // Deliberately out of alphabetical order, and not sorted by id either
  // — the API's own ordering shouldn't leak into the rendered list.
  return [
    { id: 'project-2', name: 'Widgetworks Platform' },
    { id: 'project-1', name: 'Aardvark Migration' },
    { id: 'project-3', name: 'megacorp intranet' },
  ]
}

let mountedWrappers: VueWrapper[] = []

async function mountView() {
  const wrapper = mount(ProjectListView, {
    attachTo: document.body,
    global: { plugins: [testVuetify] },
  })
  mountedWrappers.push(wrapper)
  await flushPromises()
  return wrapper
}

describe('ProjectListView', () => {
  beforeEach(() => {
    mountedWrappers = []
    pushMock.mockReset()
    listMock.mockReset().mockResolvedValue(projects())
    createMock.mockReset()
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
    document.body.innerHTML = ''
  })

  it('renders projects alphabetically by name regardless of API order', async () => {
    const wrapper = await mountView()

    const titles = wrapper.findAll('.v-list-item-title').map((t) => t.text())
    // localeCompare is case-insensitive for letter ordering, so the
    // lowercase "megacorp intranet" still sorts between the other two.
    expect(titles).toEqual(['Aardvark Migration', 'megacorp intranet', 'Widgetworks Platform'])
  })

  it('navigates to the clicked project', async () => {
    const wrapper = await mountView()

    const items = wrapper.findAll('.v-list-item')
    const aardvark = items.find((i) => i.text().includes('Aardvark Migration'))
    await aardvark!.trigger('click')

    expect(pushMock).toHaveBeenCalledWith('/projects/project-1')
  })
})
