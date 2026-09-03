<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { setupApi, type CompleteSetupPayload, type SetupFieldError } from '@/api/setup'
import { useSetupStore } from '@/stores/setup'
import { ApiError } from '@/api/httpClient'

const router = useRouter()
const setupStore = useSetupStore()

// Defaults mirror the values this deployment used to carry in
// appsettings.json/docker-compose.yml before they moved here — Host=localhost
// is correct for a bare `dotnet run`; inside Docker Compose, change it to
// Host=db (see docker-compose.yml's own comment).
const form = reactive({
  queryDbConnection:
    'Host=localhost;Port=5432;Database=architecturetoolkit;Username=user@user.com;Password=Password123',
  commandDbConnection:
    'Host=localhost;Port=5432;Database=architecturetoolkit;Username=user@user.com;Password=Password123',
  templateLibraryRootPath: '../DocumentationTemplates',
  authority: '',
  clientId: 'architecturetoolkit-spa',
  audience: 'architecturetoolkit-api',
  smtpHost: '',
  smtpPort: 587,
  smtpUsername: '',
  smtpPassword: '',
  smtpFromAddress: 'no-reply@architecturetoolkit.local',
  smtpFromName: 'Architecture Toolkit',
  smtpUseSslOnConnect: false,
  initialUserEmail: '',
  initialUserPassword: '',
  initialUserConfirmPassword: '',
})

type Phase = 'form' | 'submitting' | 'restarting' | 'restart-failed'
const phase = ref<Phase>('form')
const generalError = ref<string | null>(null)
const fieldErrors = reactive<Record<string, string>>({})

// Required-field rundown matches SetupCompletionService.Validate exactly
// (field names are the CompleteSetupRequest/PascalCase ones the backend
// reports errors against) — client-side validation here is purely a
// faster first pass; the backend re-validates everything regardless.
const requiredFields: Array<[key: keyof typeof form, field: string, label: string]> = [
  ['queryDbConnection', 'QueryDbConnection', 'Query connection string'],
  ['commandDbConnection', 'CommandDbConnection', 'Command connection string'],
  ['templateLibraryRootPath', 'TemplateLibraryRootPath', 'Template library root path'],
  ['clientId', 'ClientId', 'Client ID'],
  ['audience', 'Audience', 'Audience'],
  ['smtpFromAddress', 'SmtpFromAddress', 'SMTP from address'],
  ['smtpFromName', 'SmtpFromName', 'SMTP from name'],
  ['initialUserEmail', 'InitialUserEmail', 'Email'],
  ['initialUserPassword', 'InitialUserPassword', 'Password'],
]

function clearErrors() {
  generalError.value = null
  for (const key of Object.keys(fieldErrors)) {
    delete fieldErrors[key]
  }
}

function validateLocally(): boolean {
  clearErrors()
  let valid = true

  for (const [key, field, label] of requiredFields) {
    if (!String(form[key]).trim()) {
      fieldErrors[field] = `${label} is required.`
      valid = false
    }
  }

  if (form.smtpPort < 1 || form.smtpPort > 65535) {
    fieldErrors.SmtpPort = 'SMTP port must be between 1 and 65535.'
    valid = false
  }

  if (form.initialUserPassword && form.initialUserPassword !== form.initialUserConfirmPassword) {
    fieldErrors.InitialUserConfirmPassword = 'Password and confirmation do not match.'
    valid = false
  }

  return valid
}

function applyServerErrors(errors: SetupFieldError[]) {
  clearErrors()
  for (const error of errors) {
    fieldErrors[error.field] = error.message
  }
  generalError.value = 'Please correct the highlighted fields.'
}

function toPayload(): CompleteSetupPayload {
  return {
    QueryDbConnection: form.queryDbConnection,
    CommandDbConnection: form.commandDbConnection,
    TemplateLibraryRootPath: form.templateLibraryRootPath,
    Authority: form.authority.trim() || null,
    ClientId: form.clientId,
    Audience: form.audience,
    SmtpHost: form.smtpHost.trim() || null,
    SmtpPort: form.smtpPort,
    SmtpUsername: form.smtpUsername.trim() || null,
    SmtpPassword: form.smtpPassword.trim() || null,
    SmtpFromAddress: form.smtpFromAddress,
    SmtpFromName: form.smtpFromName,
    SmtpUseSslOnConnect: form.smtpUseSslOnConnect,
    InitialUserEmail: form.initialUserEmail,
    InitialUserPassword: form.initialUserPassword,
    InitialUserConfirmPassword: form.initialUserConfirmPassword,
  }
}

async function submit() {
  if (!validateLocally()) {
    return
  }

  phase.value = 'submitting'
  try {
    await setupApi.complete(toPayload())
    // The API is about to stop its own process (SetupCompletionService)
    // so Docker's restart policy can bring it back up already
    // configured — see pollUntilBackUp.
    phase.value = 'restarting'
    await pollUntilBackUp()
  } catch (err) {
    if (err instanceof ApiError && err.status === 400) {
      const body = err.body as { errors?: SetupFieldError[] } | undefined
      applyServerErrors(body?.errors ?? [])
    } else if (err instanceof ApiError && err.status === 409) {
      generalError.value = 'Setup has already been completed. Reloading…'
      setupStore.markConfigured()
      await router.replace({ name: 'home' })
      return
    } else {
      generalError.value = 'Could not reach the server. Please try again.'
    }
    phase.value = 'form'
  }
}

const POLL_INTERVAL_MS = 2000
const POLL_INITIAL_DELAY_MS = 3000
const MAX_POLL_ATTEMPTS = 60 // ~2 minutes total, generous for a cold container pull/start

function delay(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms))
}

/**
 * The API process is restarting itself (SetupCompletionService), so this
 * request itself has nothing to wait on — polling /api/setup/status is
 * how the SPA finds out the new process is back up and fully configured.
 * A brief initial delay avoids a first attempt racing the shutdown
 * that's already in flight.
 */
async function pollUntilBackUp() {
  await delay(POLL_INITIAL_DELAY_MS)

  for (let attempt = 0; attempt < MAX_POLL_ATTEMPTS; attempt++) {
    try {
      const status = await setupApi.status()
      if (status.isConfigured) {
        setupStore.markConfigured()
        await router.replace({ name: 'home' })
        return
      }
    } catch {
      // Expected mid-restart (connection refused/reset) — just keep polling.
    }
    await delay(POLL_INTERVAL_MS)
  }

  phase.value = 'restart-failed'
}

const isSubmitting = computed(() => phase.value === 'submitting')

function reload() {
  window.location.reload()
}
</script>

<template>
  <v-container class="d-flex justify-center py-8">
    <div style="max-width: 720px; width: 100%">
      <template v-if="phase === 'form' || phase === 'submitting'">
        <h1 class="text-h4 mb-1">Welcome to Architecture Toolkit</h1>
        <p class="text-body-2 mb-6">
          This looks like a fresh install. Fill in the details below to get started — everything
          here is stored encrypted by the API itself, not in a config file.
        </p>

        <v-alert v-if="generalError" type="error" :text="generalError" class="mb-4" />

        <v-card title="Deployment Configuration" class="mb-6">
          <v-card-text>
            <v-text-field
              id="setup-query-db-connection"
              v-model="form.queryDbConnection"
              label="Query connection string"
              :error-messages="fieldErrors.QueryDbConnection"
              class="mb-2"
            />
            <v-text-field
              id="setup-command-db-connection"
              v-model="form.commandDbConnection"
              label="Command connection string"
              :error-messages="fieldErrors.CommandDbConnection"
              class="mb-2"
            />
            <v-text-field
              id="setup-template-library-root-path"
              v-model="form.templateLibraryRootPath"
              label="Template library root path"
              :error-messages="fieldErrors.TemplateLibraryRootPath"
              class="mb-2"
            />

            <v-divider class="mb-4" />

            <v-text-field
              id="setup-authority"
              v-model="form.authority"
              label="Authority (leave blank for the built-in identity provider)"
              :error-messages="fieldErrors.Authority"
              class="mb-2"
            />
            <v-text-field
              id="setup-client-id"
              v-model="form.clientId"
              label="Client ID"
              :error-messages="fieldErrors.ClientId"
              class="mb-2"
            />
            <v-text-field
              id="setup-audience"
              v-model="form.audience"
              label="Audience"
              :error-messages="fieldErrors.Audience"
              class="mb-2"
            />

            <v-divider class="mb-4" />

            <v-text-field
              id="setup-smtp-host"
              v-model="form.smtpHost"
              label="SMTP host (leave blank to disable email)"
              :error-messages="fieldErrors.SmtpHost"
              class="mb-2"
            />
            <v-text-field
              id="setup-smtp-port"
              v-model.number="form.smtpPort"
              label="SMTP port"
              type="number"
              :error-messages="fieldErrors.SmtpPort"
              class="mb-2"
            />
            <v-text-field
              id="setup-smtp-username"
              v-model="form.smtpUsername"
              label="SMTP username"
              :error-messages="fieldErrors.SmtpUsername"
              class="mb-2"
            />
            <v-text-field
              id="setup-smtp-password"
              v-model="form.smtpPassword"
              label="SMTP password"
              type="password"
              :error-messages="fieldErrors.SmtpPassword"
              class="mb-2"
            />
            <v-text-field
              id="setup-smtp-from-address"
              v-model="form.smtpFromAddress"
              label="SMTP from address"
              :error-messages="fieldErrors.SmtpFromAddress"
              class="mb-2"
            />
            <v-text-field
              id="setup-smtp-from-name"
              v-model="form.smtpFromName"
              label="SMTP from name"
              :error-messages="fieldErrors.SmtpFromName"
              class="mb-2"
            />
            <v-switch
              id="setup-smtp-use-ssl-on-connect"
              v-model="form.smtpUseSslOnConnect"
              label="Use SSL on connect"
              :error-messages="fieldErrors.SmtpUseSslOnConnect"
              color="accent"
            />
          </v-card-text>
        </v-card>

        <v-card title="Initial User Account" class="mb-6">
          <v-card-text>
            <v-text-field
              id="setup-initial-user-email"
              v-model="form.initialUserEmail"
              label="Email"
              :error-messages="fieldErrors.InitialUserEmail"
              class="mb-2"
            />
            <v-text-field
              id="setup-initial-user-password"
              v-model="form.initialUserPassword"
              label="Password"
              type="password"
              :error-messages="fieldErrors.InitialUserPassword"
              class="mb-2"
            />
            <v-text-field
              id="setup-initial-user-confirm-password"
              v-model="form.initialUserConfirmPassword"
              label="Confirm password"
              type="password"
              :error-messages="fieldErrors.InitialUserConfirmPassword"
              @keyup.enter="submit"
            />
          </v-card-text>
        </v-card>

        <v-btn id="setup-save" color="accent" block size="large" :loading="isSubmitting" @click="submit">
          Save
        </v-btn>
      </template>

      <template v-else-if="phase === 'restarting'">
        <div class="d-flex flex-column align-center text-center py-16">
          <v-progress-circular indeterminate color="accent" size="64" class="mb-6" />
          <h2 class="text-h5 mb-2">Setup complete — restarting</h2>
          <p class="text-body-2">
            The app is applying your configuration and will be back in a moment.
          </p>
        </div>
      </template>

      <template v-else-if="phase === 'restart-failed'">
        <v-alert type="warning" class="mb-4">
          Setup was saved, but the app hasn't come back up yet. If this is a Docker deployment,
          check that the container has a restart policy configured — otherwise, restart it
          manually. Then reload this page.
        </v-alert>
        <v-btn id="setup-reload" color="accent" block @click="reload">Reload</v-btn>
      </template>
    </div>
  </v-container>
</template>
