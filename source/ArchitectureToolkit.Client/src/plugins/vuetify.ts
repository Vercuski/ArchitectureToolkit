// Self-hosted, no external CDN dependency — same pattern as the MDI
// icon font below. Weights match Vuetify's own default typography scale
// (text-h1..text-overline, buttons, emphasis) without pulling in the
// rarely-used 100/900 extremes.
import '@fontsource/roboto/300.css'
import '@fontsource/roboto/400.css'
import '@fontsource/roboto/500.css'
import '@fontsource/roboto/700.css'
import '@mdi/font/css/materialdesignicons.css'
import { createVuetify } from 'vuetify'
import { getStoredTheme, THEME_DEFINITIONS } from '@/theme/themes'

export default createVuetify({
  theme: {
    defaultTheme: getStoredTheme(),
    themes: THEME_DEFINITIONS,
  },
})
