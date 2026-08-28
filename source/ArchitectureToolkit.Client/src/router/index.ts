import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import CallbackView from '../views/CallbackView.vue'
import ProjectListView from '../views/ProjectListView.vue'
import ProjectDetailView from '../views/ProjectDetailView.vue'
import TemplateListView from '../views/TemplateListView.vue'
import TemplateDetailView from '../views/TemplateDetailView.vue'
import DocumentDetailView from '../views/DocumentDetailView.vue'
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
      path: '/projects',
      name: 'projects',
      component: ProjectListView,
      meta: { requiresAuth: true },
    },
    {
      path: '/projects/:id',
      name: 'project-detail',
      component: ProjectDetailView,
      meta: { requiresAuth: true },
    },
    {
      path: '/templates',
      name: 'templates',
      component: TemplateListView,
      meta: { requiresAuth: true },
    },
    {
      path: '/templates/:id',
      name: 'template-detail',
      component: TemplateDetailView,
      meta: { requiresAuth: true },
    },
    {
      // Not nested under /projects/:id — ProjectDocument has its own
      // global identity (~/api/documents/{id} on the backend), and the
      // document's own projectId (from ProjectDocumentDetailDto) is the
      // single source of truth for "which project," not the URL too.
      path: '/documents/:id',
      name: 'document-detail',
      component: DocumentDetailView,
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
    // to.fullPath is carried through login() so CallbackView can send the
    // user back to the page they actually asked for, not just '/'.
    await authStore.login(to.fullPath)
    return false
  }

  return true
})

export default router
