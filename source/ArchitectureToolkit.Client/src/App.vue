<script setup lang="ts">
import { RouterView, useRoute } from 'vue-router'
import SideNav from '@/components/SideNav.vue'

// The setup wizard (see router/index.ts) has nothing to navigate to yet —
// no side nav for a deployment that isn't configured, or isn't logged in
// to anything, at all.
const route = useRoute()
</script>

<template>
  <v-app>
    <SideNav v-if="route.name !== 'setup'" />
    <v-main>
      <RouterView />
    </v-main>
  </v-app>
</template>
