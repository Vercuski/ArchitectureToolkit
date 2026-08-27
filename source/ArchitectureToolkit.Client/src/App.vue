<script setup lang="ts">
import { RouterView, RouterLink } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <v-app>
    <v-app-bar>
      <v-app-bar-title>
        <RouterLink to="/" class="text-white text-decoration-none">ArchitectureToolkit</RouterLink>
      </v-app-bar-title>
      <v-btn v-if="authStore.isAuthenticated" to="/projects" variant="text">Projects</v-btn>
      <v-btn v-if="authStore.isAuthenticated" to="/templates" variant="text">Templates</v-btn>
      <v-spacer />
      <v-btn v-if="authStore.isAuthenticated" @click="authStore.logout()">Sign out</v-btn>
    </v-app-bar>
    <v-main>
      <RouterView />
    </v-main>
  </v-app>
</template>
