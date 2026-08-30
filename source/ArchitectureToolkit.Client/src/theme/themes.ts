import type { ThemeDefinition } from 'vuetify'

export const THEME_NAMES = ['slateBlue', 'indigoTeal', 'charcoalAmber', 'forestSlate'] as const

export type ThemeName = (typeof THEME_NAMES)[number]

export const THEME_LABELS: Record<ThemeName, string> = {
  slateBlue: 'Deep Slate Blue',
  indigoTeal: 'Indigo + Teal',
  charcoalAmber: 'Charcoal + Amber',
  forestSlate: 'Forest + Slate',
}

/**
 * One representative color per theme, for the switcher's own UI — the
 * theme's most visually distinguishing color, not necessarily its
 * `primary`. For slateBlue/indigoTeal/forestSlate that's the primary
 * itself; for charcoalAmber, primary is a deliberately neutral charcoal,
 * so the accent is what actually differentiates it in a picker.
 */
export const THEME_SWATCHES: Record<ThemeName, string> = {
  slateBlue: '#1E3A5F',
  indigoTeal: '#4338CA',
  charcoalAmber: '#F59E0B',
  forestSlate: '#1B4332',
}

// Semantic status colors (error/info/success/warning) are deliberately
// identical across every theme below — they carry meaning users rely on
// regardless of which palette is active, so only the structural/brand
// colors (primary, secondary, background, surface, accent) vary per theme.
const semanticColors = {
  error: '#B00020',
  info: '#2196F3',
  success: '#4CAF50',
  warning: '#FB8C00',
}

// `accent` isn't one of Vuetify's built-in theme keys, but any custom key
// added here gets the same treatment (a --v-theme-accent CSS variable and
// an auto-computed contrasting on-accent) — used for primary CTA buttons
// (Create/Save/New, etc.) across the app via color="accent". For themes
// whose `primary` already reads as a strong CTA color, accent just equals
// primary; for the two neutral-primary themes, it's the distinct color
// that actually was the point of choosing that theme.
//
// Single source of truth for both plugins/vuetify.ts (the real app) and
// test-utils/vuetify.ts (component tests) — defining these twice would
// let the two drift out of sync silently.
export const THEME_DEFINITIONS: Record<ThemeName, ThemeDefinition> = {
  slateBlue: {
    dark: false,
    colors: {
      ...semanticColors,
      background: '#F8FAFC',
      surface: '#FFFFFF',
      'surface-variant': '#E2E8F0',
      'on-surface-variant': '#1E293B',
      primary: '#1E3A5F',
      'primary-darken-1': '#14293F',
      secondary: '#64748B',
      'secondary-darken-1': '#475569',
      accent: '#1E3A5F',
    },
  },
  indigoTeal: {
    dark: false,
    colors: {
      ...semanticColors,
      background: '#F8FAFC',
      surface: '#FFFFFF',
      'surface-variant': '#E0E7FF',
      'on-surface-variant': '#312E81',
      primary: '#4338CA',
      'primary-darken-1': '#3730A3',
      secondary: '#0D9488',
      'secondary-darken-1': '#0F766E',
      accent: '#4338CA',
    },
  },
  charcoalAmber: {
    dark: false,
    colors: {
      ...semanticColors,
      background: '#FAFAFA',
      surface: '#FFFFFF',
      'surface-variant': '#F4F4F5',
      'on-surface-variant': '#27272A',
      primary: '#27272A',
      'primary-darken-1': '#18181B',
      secondary: '#52525B',
      'secondary-darken-1': '#3F3F46',
      accent: '#F59E0B',
    },
  },
  forestSlate: {
    dark: false,
    colors: {
      ...semanticColors,
      background: '#F8F9FA',
      surface: '#FFFFFF',
      'surface-variant': '#E9ECEF',
      'on-surface-variant': '#212529',
      primary: '#1B4332',
      'primary-darken-1': '#14342A',
      secondary: '#495057',
      'secondary-darken-1': '#343A40',
      accent: '#C9A227',
    },
  },
}

const DEFAULT_THEME: ThemeName = 'slateBlue'
const STORAGE_KEY = 'architecturetoolkit:theme'

function isThemeName(value: string | null): value is ThemeName {
  return !!value && (THEME_NAMES as readonly string[]).includes(value)
}

/**
 * Read synchronously so plugins/vuetify.ts can set the correct
 * `defaultTheme` at createVuetify() time — the theme has to be right
 * before the app's first paint, not patched in after a reactive update,
 * or a returning visitor briefly sees the wrong palette flash by.
 */
export function getStoredTheme(): ThemeName {
  const stored = localStorage.getItem(STORAGE_KEY)
  return isThemeName(stored) ? stored : DEFAULT_THEME
}

export function setStoredTheme(name: ThemeName): void {
  localStorage.setItem(STORAGE_KEY, name)
}
