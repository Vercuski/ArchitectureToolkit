import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

// Shared across component tests: real Vuetify components/directives, no
// theme/styles import — not needed to test behavior, and importing
// vuetify/styles would pull in the full compiled stylesheet for nothing.
export const testVuetify = createVuetify({ components, directives })
