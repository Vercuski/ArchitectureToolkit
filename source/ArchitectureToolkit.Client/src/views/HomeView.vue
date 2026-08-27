<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <v-container>
    <v-card>
      <v-card-title>Welcome to ArchitectureToolkit</v-card-title>
      <v-card-text>
        <p>Signed in as {{ authStore.user?.profile.email ?? authStore.user?.profile.sub }}</p>
      </v-card-text>
    </v-card>
  </v-container>
</template>
