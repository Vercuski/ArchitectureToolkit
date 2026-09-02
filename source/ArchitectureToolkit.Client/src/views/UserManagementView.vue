<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { usersApi } from '@/api/users'
import { authApi } from '@/api/auth'
import { ApiError } from '@/api/httpClient'
import type { SystemRole, UserManagementDto } from '@/api/types'

const users = ref<UserManagementDto[]>([])
const loading = ref(true)
const loadError = ref<string | null>(null)

const rowBusy = ref<string | null>(null)
const rowError = ref<string | null>(null)

// Mirrors HomeView's copy-to-clipboard feedback, keyed per row since this
// is a table of many users rather than a single profile.
const copiedUserId = ref<string | null>(null)

// ADR-0018: only meaningful for self-hosted deployments — an external
// Authority manages its own accounts, so the button stays hidden there
// rather than only failing after the form is filled out.
const supportsPasswordAccounts = ref(false)

const createDialogOpen = ref(false)
const newEmail = ref('')
const newRole = ref<SystemRole>('Contributor')
const creating = ref(false)
const createError = ref<string | null>(null)

const inviteOutcome = ref<{ email: string; emailSent: boolean; inviteLink: string | null } | null>(null)
const inviteLinkCopied = ref(false)

function apiErrorMessage(err: unknown, fallback: string): string {
  return err instanceof ApiError ? ((err.body as { error?: string })?.error ?? fallback) : fallback
}

async function load() {
  loading.value = true
  loadError.value = null
  try {
    // Server already sorts by email (ListUsersQueryHandler) — no
    // client-side re-sort needed.
    users.value = await usersApi.list()
  } catch (err) {
    // Also covers the 403 a non-architect gets here — the API is the
    // real enforcement point (ADR-0017), this is just presenting its
    // answer rather than a route-level gate.
    loadError.value = apiErrorMessage(err, 'Failed to load users.')
  } finally {
    loading.value = false
  }
}

async function loadAuthConfig() {
  try {
    const config = await authApi.getConfig()
    supportsPasswordAccounts.value = config.useSelfHostedProvider
  } catch {
    // Fails closed: if we can't confirm this is a self-hosted deployment,
    // the button stays hidden rather than risk offering an action the API
    // will refuse anyway.
    supportsPasswordAccounts.value = false
  }
}

async function copyUserId(userId: string) {
  await navigator.clipboard.writeText(userId)
  copiedUserId.value = userId
  setTimeout(() => {
    if (copiedUserId.value === userId) {
      copiedUserId.value = null
    }
  }, 1500)
}

async function copyInviteLink(link: string) {
  await navigator.clipboard.writeText(link)
  inviteLinkCopied.value = true
  setTimeout(() => {
    inviteLinkCopied.value = false
  }, 1500)
}

async function toggleActive(user: UserManagementDto) {
  rowBusy.value = user.id
  rowError.value = null
  const previousStatus = user.isActive
  const nextStatus = !user.isActive
  try {
    const updated = await usersApi.setActive(user.id, nextStatus)
    user.isActive = updated.isActive
  } catch (err) {
    user.isActive = previousStatus
    rowError.value = apiErrorMessage(err, 'Failed to update active status.')
  } finally {
    rowBusy.value = null
  }
}

async function createUser() {
  const email = newEmail.value.trim()
  if (!email) {
    return
  }
  creating.value = true
  createError.value = null
  try {
    const result = await usersApi.create(email, newRole.value)
    createDialogOpen.value = false
    newEmail.value = ''
    newRole.value = 'Contributor'
    inviteOutcome.value = { email, emailSent: result.emailSent, inviteLink: result.inviteLink }
    await load()
  } catch (err) {
    createError.value = apiErrorMessage(err, 'Failed to create user.')
  } finally {
    creating.value = false
  }
}

onMounted(() => {
  load()
  loadAuthConfig()
})
</script>

<template>
  <v-container>
    <div class="d-flex align-center justify-space-between mb-4">
      <h1 class="text-h5">User Management</h1>
      <v-btn
        v-if="supportsPasswordAccounts"
        id="new-user-button"
        color="accent"
        prepend-icon="mdi-account-plus"
        @click="createDialogOpen = true"
      >
        New User
      </v-btn>
    </div>

    <v-alert
      v-if="inviteOutcome"
      type="success"
      variant="tonal"
      closable
      class="mb-4"
      @click:close="inviteOutcome = null"
    >
      <template v-if="inviteOutcome.emailSent">
        Invite email sent to {{ inviteOutcome.email }}.
      </template>
      <template v-else>
        <div class="mb-2">
          SMTP isn't configured (or the send failed) — share this link with
          {{ inviteOutcome.email }} so they can set their password:
        </div>
        <div class="d-flex align-center ga-2">
          <code class="text-caption">{{ inviteOutcome.inviteLink }}</code>
          <v-btn
            size="small"
            variant="text"
            :prepend-icon="inviteLinkCopied ? 'mdi-check' : 'mdi-content-copy'"
            @click="copyInviteLink(inviteOutcome.inviteLink!)"
          >
            {{ inviteLinkCopied ? 'Copied!' : 'Copy link' }}
          </v-btn>
        </div>
      </template>
    </v-alert>

    <v-progress-linear v-if="loading" indeterminate class="mb-4" />
    <v-alert v-if="loadError" type="error" :text="loadError" />

    <template v-if="!loading && !loadError">
      <v-alert v-if="rowError" type="error" :text="rowError" class="mb-4" />

      <v-card>
        <v-table>
          <thead>
            <tr>
              <th>Email</th>
              <th>User ID</th>
              <th>Active</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="user in users" :key="user.id">
              <td>{{ user.email }}</td>
              <td>
                <div class="d-flex align-center ga-1">
                  <code class="text-caption">{{ user.id }}</code>
                  <v-btn
                    :icon="copiedUserId === user.id ? 'mdi-check' : 'mdi-content-copy'"
                    variant="text"
                    size="small"
                    density="compact"
                    :title="copiedUserId === user.id ? 'Copied!' : 'Copy User ID'"
                    @click="copyUserId(user.id)"
                  />
                </div>
              </td>
              <td>
                <v-switch
                  :model-value="user.isActive"
                  color="primary"
                  density="compact"
                  hide-details
                  :disabled="rowBusy === user.id"
                  @update:model-value="toggleActive(user)"
                />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <v-dialog v-model="createDialogOpen" max-width="480">
      <v-card title="New User">
        <v-card-text>
          <v-alert v-if="createError" type="error" :text="createError" class="mb-4" />
          <v-text-field
            id="new-user-email"
            v-model="newEmail"
            label="Email"
            type="email"
            class="mb-2"
          />
          <v-select
            id="new-user-role"
            v-model="newRole"
            :items="['Contributor', 'Architect']"
            label="Role"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="createDialogOpen = false">Cancel</v-btn>
          <v-btn id="confirm-create-user" color="accent" :loading="creating" @click="createUser">
            Create
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>
