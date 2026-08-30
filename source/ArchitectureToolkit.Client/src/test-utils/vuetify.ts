import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { THEME_DEFINITIONS } from '@/theme/themes'

// Shared across component tests: real Vuetify components/directives, no
// theme/styles import — not needed to test behavior, and importing
// vuetify/styles would pull in the full compiled stylesheet for nothing.
// Registers the real app's themes (not just a bare default) so any test
// that switches or reads theme.global.name — like ThemeSwitcher's —
// exercises Vuetify's actual theme-lookup logic instead of crashing on
// an unrecognized name.
export const testVuetify = createVuetify({ components, directives, theme: { themes: THEME_DEFINITIONS } })
