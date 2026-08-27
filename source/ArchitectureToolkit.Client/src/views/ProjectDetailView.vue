<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { projectsApi } from '@/api/projects'
import { ApiError } from '@/api/httpClient'
import type { ProjectDto, ProjectMemberDto, ProjectRole } from '@/api/types'

const route = useRoute()
const authStore = useAuthStore()
const projectId = route.params.id as string

const project = ref<ProjectDto | null>(null)
const members = ref<ProjectMemberDto[]>([])
const loading = ref(true)
const loadError = ref<string | null>(null)

// The backend has no concept of "my own user id" the SPA can read
// directly from the token (that mapping only exists server-side via
// IUserProvisioningService) — matching by email, which is already in
// both the ID token and every ProjectMemberDto, is the pragmatic way to
// find "me" in the member list. See GetCurrentUserQueryHandler's own doc
// comment for the same reasoning from the other direction.
const myEmail = computed(() => authStore.user?.profile.email)
const myMembership = computed(() => members.value.find((m) => m.userEmail === myEmail.value))
const isOwner = computed(() => myMembership.value?.role === 'Owner')

const roleOptions: ProjectRole[] = ['Viewer', 'Editor', 'Owner']

const addUserId = ref('')
const addRole = ref<ProjectRole>('Viewer')
const addBusy = ref(false)
const addError = ref<string | null>(null)

const rowBusy = ref<string | null>(null)
const rowError = ref<string | null>(null)

async function load() {
  loading.value = true
  loadError.value = null
  try {
    const [projectResult, membersResult] = await Promise.all([
      projectsApi.get(projectId),
      projectsApi.listMembers(projectId),
    ])
    project.value = projectResult
    members.value = membersResult
  } catch (err) {
    loadError.value = err instanceof Error ? err.message : 'Failed to load project.'
  } finally {
    loading.value = false
  }
}

function apiErrorMessage(err: unknown, fallback: string): string {
  return err instanceof ApiError ? ((err.body as { error?: string })?.error ?? fallback) : fallback
}

async function addMember() {
  if (!addUserId.value.trim()) {
    return
  }
  addBusy.value = true
  addError.value = null
  try {
    const added = await projectsApi.addMember(projectId, addUserId.value.trim(), addRole.value)
    members.value.push(added)
    addUserId.value = ''
    addRole.value = 'Viewer'
  } catch (err) {
    addError.value = apiErrorMessage(err, 'Failed to add member.')
  } finally {
    addBusy.value = false
  }
}

async function changeRole(member: ProjectMemberDto, newRole: ProjectRole) {
  rowBusy.value = member.userId
  rowError.value = null
  const previousRole = member.role
  try {
    const updated = await projectsApi.updateMemberRole(projectId, member.userId, newRole)
    member.role = updated.role
  } catch (err) {
    member.role = previousRole
    rowError.value = apiErrorMessage(err, 'Failed to update role.')
  } finally {
    rowBusy.value = null
  }
}

async function removeMember(member: ProjectMemberDto) {
  rowBusy.value = member.userId
  rowError.value = null
  try {
    await projectsApi.removeMember(projectId, member.userId)
    members.value = members.value.filter((m) => m.userId !== member.userId)
  } catch (err) {
    rowError.value = apiErrorMessage(err, 'Failed to remove member.')
  } finally {
    rowBusy.value = null
  }
}

onMounted(load)
</script>

<template>
  <v-container>
    <v-progress-linear v-if="loading" indeterminate class="mb-4" />
    <v-alert v-if="loadError" type="error" :text="loadError" />

    <template v-if="!loading && !loadError && project">
      <h1 class="text-h5 mb-4">{{ project.name }}</h1>

      <v-card title="Members">
        <v-alert v-if="rowError" type="error" :text="rowError" class="ma-4" />

        <v-table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th v-if="isOwner" class="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="member in members" :key="member.userId">
              <td>{{ member.userName }}</td>
              <td>{{ member.userEmail }}</td>
              <td>
                <v-select
                  v-if="isOwner"
                  :model-value="member.role"
                  :items="roleOptions"
                  density="compact"
                  hide-details
                  variant="underlined"
                  style="max-width: 140px"
                  :disabled="rowBusy === member.userId"
                  @update:model-value="(value) => changeRole(member, value as ProjectRole)"
                />
                <span v-else>{{ member.role }}</span>
              </td>
              <td v-if="isOwner" class="text-right">
                <v-btn
                  icon="mdi-delete-outline"
                  variant="text"
                  size="small"
                  :loading="rowBusy === member.userId"
                  @click="removeMember(member)"
                />
              </td>
            </tr>
          </tbody>
        </v-table>

        <v-card-text v-if="isOwner">
          <v-divider class="mb-4" />
          <v-alert v-if="addError" type="error" :text="addError" class="mb-4" />
          <div class="d-flex align-center ga-2">
            <v-text-field
              id="add-member-user-id"
              v-model="addUserId"
              label="User ID"
              hint="Ask the person to share their User ID from their account page"
              persistent-hint
              density="compact"
              hide-details="auto"
            />
            <v-select
              v-model="addRole"
              :items="roleOptions"
              label="Role"
              density="compact"
              hide-details
              style="max-width: 140px"
            />
            <v-btn id="confirm-add-member" color="primary" :loading="addBusy" @click="addMember">Add</v-btn>
          </div>
        </v-card-text>
      </v-card>
    </template>
  </v-container>
</template>
