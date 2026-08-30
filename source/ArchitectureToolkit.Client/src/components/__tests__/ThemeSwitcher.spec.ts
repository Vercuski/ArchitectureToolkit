import { beforeEach, describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { testVuetify } from '@/test-utils/vuetify'
import { THEME_LABELS } from '@/theme/themes'
import ThemeSwitcher from '../ThemeSwitcher.vue'

function mountSwitcher() {
  return mount(ThemeSwitcher, {
    attachTo: document.body,
    global: { plugins: [testVuetify] },
  })
}

describe('ThemeSwitcher', () => {
  beforeEach(() => {
    localStorage.clear()
    document.body.innerHTML = ''
  })

  it('lists all four theme options when opened', async () => {
    const wrapper = mountSwitcher()
    await wrapper.find('#theme-switcher-button').trigger('click')

    for (const label of Object.values(THEME_LABELS)) {
      expect(document.body.textContent).toContain(label)
    }

    wrapper.unmount()
  })

  it('selecting a theme updates the active theme and persists the choice', async () => {
    const wrapper = mountSwitcher()
    await wrapper.find('#theme-switcher-button').trigger('click')
    await document.body.querySelector('#theme-option-indigoTeal')?.dispatchEvent(new Event('click', { bubbles: true }))
    await wrapper.vm.$nextTick()

    expect(testVuetify.theme.global.name.value).toBe('indigoTeal')
    expect(localStorage.getItem('architecturetoolkit:theme')).toBe('indigoTeal')

    wrapper.unmount()
  })
})
