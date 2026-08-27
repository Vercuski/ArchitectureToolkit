<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <v-app>
    <v-app-bar>
      <v-app-bar-title>ArchitectureToolkit</v-app-bar-title>
      <v-spacer />
      <v-btn v-if="authStore.isAuthenticated" @click="authStore.logout()">Sign out</v-btn>
    </v-app-bar>
    <v-main>
      <RouterView />
    </v-main>
  </v-app>
</template>
