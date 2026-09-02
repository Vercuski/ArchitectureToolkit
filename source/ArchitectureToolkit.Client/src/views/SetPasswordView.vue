<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { accountApi } from '@/api/account'
import { useAuthStore } from '@/stores/auth'
import { ApiError } from '@/api/httpClient'

const route = useRoute()
const authStore = useAuthStore()

// Both required query params — a link without them is simply malformed,
// not a case AccountController needs to distinguish from an invalid token.
const email = computed(() => String(route.query.email ?? ''))
const token = computed(() => String(route.query.token ?? ''))

const newPassword = ref('')
const confirmPassword = ref('')
const submitting = ref(false)
const submitError = ref<string | null>(null)
const success = ref(false)

function apiErrorMessage(err: unknown, fallback: string): string {
  return err instanceof ApiError ? ((err.body as { error?: string })?.error ?? fallback) : fallback
}

async function submit() {
  if (!newPassword.value || newPassword.value !== confirmPassword.value) {
    submitError.value = 'Passwords do not match.'
    return
  }
  submitting.value = true
  submitError.value = null
  try {
    await accountApi.setPassword(email.value, token.value, newPassword.value, confirmPassword.value)
    success.value = true
  } catch (err) {
    submitError.value = apiErrorMessage(err, 'Failed to set password.')
  } finally {
    submitting.value = false
  }
}

// Completes the login this whole flow was leading up to — this is what
// actually triggers UserProvisioningService's adopt-by-email fix (ADR-0018)
// and links the identity for the first time.
function signIn() {
  authStore.login()
}
</script>

<template>
  <v-container class="d-flex justify-center">
    <v-card max-width="480" width="100%" title="Set Your Password" class="mt-8">
      <v-card-text>
        <template v-if="!email || !token">
          <v-alert type="error" text="This link is missing required information." />
        </template>

        <template v-else-if="success">
          <v-alert type="success" text="Your password has been set." class="mb-4" />
          <v-btn id="sign-in-after-set-password" color="accent" block @click="signIn">Sign In</v-btn>
        </template>

        <template v-else>
          <v-alert v-if="submitError" type="error" :text="submitError" class="mb-4" />
          <v-text-field
            id="set-password-new"
            v-model="newPassword"
            label="New Password"
            type="password"
            class="mb-2"
          />
          <v-text-field
            id="set-password-confirm"
            v-model="confirmPassword"
            label="Confirm Password"
            type="password"
            @keyup.enter="submit"
          />
          <v-btn
            id="set-password-submit"
            color="accent"
            block
            class="mt-4"
            :loading="submitting"
            @click="submit"
          >
            Submit
          </v-btn>
        </template>
      </v-card-text>
    </v-card>
  </v-container>
</template>
