import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import CallbackView from '../views/CallbackView.vue'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
      meta: { requiresAuth: true },
    },
    {
      // Must match Authentication:RedirectUris on the API — see
      // src/auth/oidcConfig.ts. Deliberately not requiresAuth: this route
      // is what completes the login process, so it must be reachable
      // before the user is considered authenticated.
      path: '/auth/callback',
      name: 'auth-callback',
      component: CallbackView,
    },
  ],
})

router.beforeEach(async (to) => {
  if (!to.meta.requiresAuth) {
    return true
  }

  const authStore = useAuthStore()
  if (!authStore.isAuthenticated) {
    // Redirects the browser away entirely (signinRedirect never resolves
    // to a router destination), so the boolean return value here is only
    // to satisfy the guard's type — navigation never actually completes.
    await authStore.login()
    return false
  }

  return true
})

export default router
