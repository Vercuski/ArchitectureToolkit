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
app.use(router)
app.use(vuetify)

// Must resolve BEFORE the router's first navigation guard runs, same
// reasoning as authStore.initialize() below — otherwise even an already-
// configured deployment would see isConfigured's optimistic default
// racing the guard on first load.
const setupStore = useSetupStore()
await setupStore.checkStatus()

// Must resolve any existing session BEFORE the router's first navigation
// guard runs, or even an already-authenticated user with a valid stored
// token would see isAuthenticated === false on first load and get sent
// through the login redirect unnecessarily.
const authStore = useAuthStore()
await authStore.initialize()

app.mount('#app')
