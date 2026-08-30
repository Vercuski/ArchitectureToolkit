<script setup lang="ts">
import { RouterView } from 'vue-router'
import SideNav from '@/components/SideNav.vue'
</script>

<template>
  <v-app>
    <SideNav />
    <v-main>
      <RouterView />
    </v-main>
  </v-app>
</template>
