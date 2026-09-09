<script setup lang="ts">
import { onMounted, ref } from 'vue'
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

// Same shape as the "Discard changes?" confirm dialogs already used on
// the Revise*/CreateDocument views — sign-out is a single, one-way click
// with no undo, sitting right next to Home/Projects/Templates in the same
// nav list, so a confirmation step guards against a stray click costing
// an unsaved edit elsewhere in the app.
const signOutConfirmOpen = ref(false)

function confirmSignOut() {
  signOutConfirmOpen.value = false
  authStore.logout()
}
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

      <v-list-item
        v-if="authStore.isAuthenticated && currentUser.profile?.systemRole === 'Architect'"
        to="/admin/settings"
        prepend-icon="mdi-cog-outline"
        title="Settings"
      />

      <ThemeSwitcher />

      <v-list-item
        v-if="authStore.isAuthenticated"
        id="sign-out-button"
        prepend-icon="mdi-logout-variant"
        title="Sign out"
        @click="signOutConfirmOpen = true"
      />
      <v-list-item
        v-else
        id="sign-in-button"
        prepend-icon="mdi-login-variant"
        title="Sign in"
        @click="authStore.login()"
      />
    </v-list>

    <v-dialog v-model="signOutConfirmOpen" max-width="400">
      <v-card title="Sign out?">
        <v-card-text> Are you sure you want to sign out? </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="signOutConfirmOpen = false">Cancel</v-btn>
          <v-btn id="confirm-sign-out" color="error" @click="confirmSignOut">Sign Out</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
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
