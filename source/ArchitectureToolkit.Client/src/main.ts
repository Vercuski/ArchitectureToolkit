import './assets/main.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import vuetify from './plugins/vuetify'
import { useAuthStore } from './stores/auth'
import { useSetupStore } from './stores/setup'

const app = createApp(App)

app.use(createPinia())

// Both resolved BEFORE the router is installed (below) — vue-router's own
// install(app) calls push(routerHistory.location) synchronously as part
// of app.use(router) itself, kicking off the first run of every
// beforeEach guard immediately, independent of app.mount(). Awaiting
// these first and installing the router only afterward is what actually
// guarantees that first guard run sees real values instead of
// isConfigured/isAuthenticated's pre-fetch defaults — installing the
// router earlier and merely awaiting these before app.mount() left the
// guard's very first (synchronous, pre-await) read of the store racing
// each fetch, and the fetch could — and did — lose that race.
const setupStore = useSetupStore()
await setupStore.checkStatus()

const authStore = useAuthStore()
await authStore.initialize()

app.use(router)
app.use(vuetify)

app.mount('#app')
