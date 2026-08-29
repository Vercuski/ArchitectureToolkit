import { fileURLToPath } from 'node:url'
import { mergeConfig, defineConfig, configDefaults } from 'vitest/config'
import viteConfig from './vite.config'

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: 'jsdom',
      exclude: [...configDefaults.exclude, 'e2e/**'],
      root: fileURLToPath(new URL('./', import.meta.url)),
      setupFiles: ['./src/test-setup.ts'],
      // Vuetify's component barrel (vuetify/components) side-imports each
      // component's own .css file. Left external, Vitest loads that via
      // plain Node ESM resolution, which chokes on the .css extension —
      // inlining forces it through Vite's own transform instead, which
      // strips styles in the test environment as expected.
      server: {
        deps: {
          inline: ['vuetify'],
        },
      },
    },
  }),
)
