<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import ThemeSwitcher from '@/components/ThemeSwitcher.vue'

const authStore = useAuthStore()
</script>

<template>
  <v-navigation-drawer permanent width="248" color="grey-lighten-4">
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
