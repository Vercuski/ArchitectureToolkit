<script setup lang="ts">
import { onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useCurrentUserStore } from '@/stores/currentUser'
import ThemeSwitcher from '@/components/ThemeSwitcher.vue'

const authStore = useAuthStore()
const currentUser = useCurrentUserStore()

// SideNav is always mounted (App.vue renders it outside RouterView), so
// unlike HomeView — which only loads the profile when someone actually
// lands there — this is the one place that must trigger the load
// proactively, or a user who deep-links straight to e.g. /projects would
// never see the User Management link even if they're an architect.
// ensureLoaded() is a no-op if something already triggered it.
onMounted(() => {
  if (authStore.isAuthenticated) {
    currentUser.ensureLoaded()
  }
})
</script>

<template>
  <v-navigation-drawer permanent width="248" class="side-nav">
    <v-list nav color="primary" density="comfortable">
      <v-list-item to="/" prepend-icon="mdi-home-outline" title="Home" />

      <v-list-item
        v-if="authStore.isAuthenticated"
        to="/projects"
        prepend-icon="mdi-folder-multiple-outline"
        title="Projects"
      />

      <v-list-item
        v-if="authStore.isAuthenticated"
        to="/templates"
        prepend-icon="mdi-file-document-multiple-outline"
        title="Templates"
      />

      <v-list-item
        v-if="authStore.isAuthenticated && currentUser.profile?.systemRole === 'Architect'"
        to="/admin/users"
        prepend-icon="mdi-account-cog-outline"
        title="User Management"
      />

      <ThemeSwitcher />

      <v-list-item
        v-if="authStore.isAuthenticated"
        id="sign-out-button"
        prepend-icon="mdi-logout-variant"
        title="Sign out"
        @click="authStore.logout()"
      />
      <v-list-item
        v-else
        id="sign-in-button"
        prepend-icon="mdi-login-variant"
        title="Sign in"
        @click="authStore.login()"
      />
    </v-list>
  </v-navigation-drawer>
</template>

<style scoped>
/*
 * Deliberately NOT using the `color` prop (e.g. color="grey-lighten-4") for
 * this background. Vuetify's `color`/`bg-*` utility only defines
 * --v-theme-overlay-multiplier when the color is one of the *theme's own*
 * keys — a plain Material-palette swatch name like "grey-lighten-4" has no
 * such companion, which breaks the highlight opacity on every active/hover
 * v-list-item nested in here (see theme/themes.ts's `overlayVariables` for
 * the full explanation and the actual app-wide fix for that variable).
 * Plain CSS avoids the utility-class mechanism entirely.
 */
.side-nav {
  background-color: #f5f5f5;
}
</style>
