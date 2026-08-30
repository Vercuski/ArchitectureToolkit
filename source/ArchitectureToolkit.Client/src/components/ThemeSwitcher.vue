<script setup lang="ts">
import { useTheme } from 'vuetify'
import { THEME_NAMES, THEME_LABELS, THEME_SWATCHES, setStoredTheme, type ThemeName } from '@/theme/themes'

const theme = useTheme()

function selectTheme(name: ThemeName) {
  theme.change(name)
  setStoredTheme(name)
}
</script>

<template>
  <v-menu>
    <template #activator="{ props: menuProps }">
      <v-btn id="theme-switcher-button" icon="mdi-palette" v-bind="menuProps" />
    </template>
    <v-list density="compact">
      <v-list-item
        v-for="name in THEME_NAMES"
        :id="`theme-option-${name}`"
        :key="name"
        :active="theme.global.name.value === name"
        @click="selectTheme(name)"
      >
        <template #prepend>
          <v-avatar :color="THEME_SWATCHES[name]" size="16" />
        </template>
        <v-list-item-title class="ml-2">{{ THEME_LABELS[name] }}</v-list-item-title>
      </v-list-item>
    </v-list>
  </v-menu>
</template>
