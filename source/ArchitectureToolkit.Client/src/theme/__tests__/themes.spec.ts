import { beforeEach, describe, expect, it } from 'vitest'
import { getStoredTheme, setStoredTheme, THEME_NAMES } from '../themes'

describe('theme persistence', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('defaults to slateBlue when nothing is stored', () => {
    expect(getStoredTheme()).toBe('slateBlue')
  })

  it('defaults to slateBlue when the stored value is not a known theme name', () => {
    localStorage.setItem('architecturetoolkit:theme', 'not-a-real-theme')
    expect(getStoredTheme()).toBe('slateBlue')
  })

  it('round-trips every valid theme name through storage', () => {
    for (const name of THEME_NAMES) {
      setStoredTheme(name)
      expect(getStoredTheme()).toBe(name)
    }
  })
})
