import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { defineComponent, h } from 'vue'
import { VLayout } from 'vuetify/components'
import type { User } from 'oidc-client-ts'
import { testVuetify } from '@/test-utils/vuetify'
import type { UserDto } from '@/api/types'

const meMock = vi.fn()
vi.mock('@/api/users', () => ({ usersApi: { me: meMock } }))

const { useAuthStore } = await import('@/stores/auth')
const { default: SideNav } = await import('../SideNav.vue')

function architect(): UserDto {
  return { id: 'user-1', name: 'Ada Architect', email: 'ada@example.com', systemRole: 'Architect' }
}

function contributor(): UserDto {
  return { id: 'user-2', name: 'Cara Contributor', email: 'cara@example.com', systemRole: 'Contributor' }
}

function signIn() {
  const authStore = useAuthStore()
  // Same approach as router/__tests__/index.spec.ts: set the session
  // directly rather than exercising oidc-client-ts's own loading path.
  authStore.user = { access_token: 'token-123', expired: false } as User
}

let mountedWrappers: VueWrapper[] = []

// VNavigationDrawer (real Vuetify, not stubbed) needs a v-layout ancestor
// to resolve its layout injection — App.vue provides that in the real app
// via v-app; this thin wrapper stands in for it here.
const LayoutHost = defineComponent({
  render() {
    return h(VLayout, () => h(SideNav))
  },
})

async function mountSideNav() {
  const wrapper = mount(LayoutHost, {
    attachTo: document.body,
    global: {
      plugins: [testVuetify],
      stubs: { RouterLink: { template: '<a><slot /></a>' } },
    },
  })
  mountedWrappers.push(wrapper)
  await flushPromises()
  return wrapper
}

describe('SideNav', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mountedWrappers = []
    meMock.mockReset()
  })

  afterEach(() => {
    mountedWrappers.forEach((w) => w.unmount())
    document.body.innerHTML = ''
  })

  it('does not show User Management when signed out', async () => {
    const wrapper = await mountSideNav()

    expect(wrapper.text()).not.toContain('User Management')
    expect(meMock).not.toHaveBeenCalled()
  })

  it('does not show User Management for a signed-in Contributor', async () => {
    meMock.mockResolvedValue(contributor())
    signIn()
    const wrapper = await mountSideNav()

    expect(wrapper.text()).not.toContain('User Management')
  })

  it('shows User Management for a signed-in Architect', async () => {
    meMock.mockResolvedValue(architect())
    signIn()
    const wrapper = await mountSideNav()

    expect(wrapper.text()).toContain('User Management')
  })
})
