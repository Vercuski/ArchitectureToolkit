<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { settingsApi } from '@/api/settings'
import { pollUntilBackUp } from '@/composables/useRestartPoll'
import { ApiError } from '@/api/httpClient'
import type { SetupFieldError } from '@/api/setup'

// Mirrors SetupWizardView's own phase shape, with 'loading'/'load-error'
// added on the front (this page has existing values to fetch, unlike the
// Setup Wizard, which starts from fixed defaults) and no 'submitting' —
// 'saving' covers the same role.
type Phase = 'loading' | 'load-error' | 'form' | 'saving' | 'restarting' | 'restart-failed'
const phase = ref<Phase>('loading')
const loadError = ref<string | null>(null)

// SmtpPassword deliberately starts blank on every load — the backend
// never sends the actual password back (see SettingsDto's own doc
// comment), so there is nothing to pre-fill it with. Leaving it blank on
// save means "keep the current one"; smtpPasswordConfigured is what lets
// the form tell the operator whether that "current one" exists at all.
const form = reactive({
  templateLibraryRootPath: '',
  smtpHost: '',
  smtpPort: 587,
  smtpUsername: '',
  smtpPassword: '',
  smtpFromAddress: '',
  smtpFromName: '',
  smtpUseSslOnConnect: false,
})
const smtpPasswordConfigured = ref(false)

const generalError = ref<string | null>(null)
const fieldErrors = reactive<Record<string, string>>({})
const saveSuccess = ref(false)

function clearMessages() {
  generalError.value = null
  saveSuccess.value = false
  for (const key of Object.keys(fieldErrors)) {
    delete fieldErrors[key]
  }
}

function applyServerErrors(errors: SetupFieldError[]) {
  clearMessages()
  for (const error of errors) {
    fieldErrors[error.field] = error.message
  }
  generalError.value = 'Please correct the highlighted fields.'
}

function apiErrorMessage(err: unknown, fallback: string): string {
  return err instanceof ApiError ? ((err.body as { error?: string })?.error ?? fallback) : fallback
}

async function load() {
  phase.value = 'loading'
  loadError.value = null
  try {
    const settings = await settingsApi.get()
    form.templateLibraryRootPath = settings.templateLibraryRootPath
    form.smtpHost = settings.smtpHost ?? ''
    form.smtpPort = settings.smtpPort
    form.smtpUsername = settings.smtpUsername ?? ''
    form.smtpPassword = ''
    form.smtpFromAddress = settings.smtpFromAddress
    form.smtpFromName = settings.smtpFromName
    form.smtpUseSslOnConnect = settings.smtpUseSslOnConnect
    smtpPasswordConfigured.value = settings.smtpPasswordConfigured
    phase.value = 'form'
  } catch (err) {
    // Also covers the 403 a non-architect gets here — the API is the
    // real enforcement point (ADR-0024), this is just presenting its
    // answer, same pattern as UserManagementView's own load error.
    loadError.value = apiErrorMessage(err, 'Failed to load settings.')
    phase.value = 'load-error'
  }
}

async function save() {
  clearMessages()
  phase.value = 'saving'
  try {
    await settingsApi.update({
      TemplateLibraryRootPath: form.templateLibraryRootPath,
      SmtpHost: form.smtpHost.trim() || null,
      SmtpPort: form.smtpPort,
      SmtpUsername: form.smtpUsername.trim() || null,
      // Blank means "leave the stored password unchanged" — see
      // UpdateSettingsPayload's own doc comment. Trimmed-blank collapses
      // to undefined so httpClient's JSON.stringify omits the property
      // entirely rather than sending an explicit empty string.
      SmtpPassword: form.smtpPassword.trim() || undefined,
      SmtpFromAddress: form.smtpFromAddress,
      SmtpFromName: form.smtpFromName,
      SmtpUseSslOnConnect: form.smtpUseSslOnConnect,
    })
    // The API is about to stop its own process (SettingsService), same
    // restart-to-apply mechanism the Setup Wizard already uses — see
    // pollUntilBackUp's own doc comment.
    phase.value = 'restarting'
    const cameBackUp = await pollUntilBackUp(async () => {
      // Re-load rather than just flipping back to 'form': confirms the
      // restarted process actually persisted what was saved, and picks
      // up the freshly-current smtpPasswordConfigured/blank-password
      // state exactly as a fresh page load would.
      await load()
      saveSuccess.value = true
    })
    if (!cameBackUp) {
      phase.value = 'restart-failed'
    }
  } catch (err) {
    if (err instanceof ApiError && err.status === 400) {
      const body = err.body as { errors?: SetupFieldError[] } | undefined
      applyServerErrors(body?.errors ?? [])
    } else {
      generalError.value = apiErrorMessage(err, 'Could not reach the server. Please try again.')
    }
    phase.value = 'form'
  }
}

function reload() {
  window.location.reload()
}

onMounted(load)
</script>

<template>
  <v-container class="d-flex justify-center py-8">
    <div style="max-width: 720px; width: 100%">
      <template v-if="phase === 'loading'">
        <v-progress-linear indeterminate />
      </template>

      <template v-else-if="phase === 'load-error'">
        <v-alert type="error" :text="loadError ?? 'Failed to load settings.'" />
      </template>

      <template v-else-if="phase === 'form' || phase === 'saving'">
        <h1 class="text-h5 mb-4">Settings</h1>

        <v-alert
          v-if="saveSuccess"
          type="success"
          text="Settings saved."
          closable
          class="mb-4"
          @click:close="saveSuccess = false"
        />
        <v-alert v-if="generalError" type="error" :text="generalError" class="mb-4" />

        <v-card title="Template Library" class="mb-6">
          <v-card-text>
            <v-text-field
              id="settings-template-library-root-path"
              v-model="form.templateLibraryRootPath"
              label="Template library root path"
              :error-messages="fieldErrors.TemplateLibraryRootPath"
            />
          </v-card-text>
        </v-card>

        <v-card title="Outbound Email (SMTP)" class="mb-6">
          <v-card-text>
            <v-text-field
              id="settings-smtp-host"
              v-model="form.smtpHost"
              label="SMTP host (leave blank to disable email)"
              :error-messages="fieldErrors.SmtpHost"
              class="mb-2"
            />
            <v-text-field
              id="settings-smtp-port"
              v-model.number="form.smtpPort"
              label="SMTP port"
              type="number"
              :error-messages="fieldErrors.SmtpPort"
              class="mb-2"
            />
            <v-text-field
              id="settings-smtp-username"
              v-model="form.smtpUsername"
              label="SMTP username"
              :error-messages="fieldErrors.SmtpUsername"
              class="mb-2"
            />
            <v-text-field
              id="settings-smtp-password"
              v-model="form.smtpPassword"
              label="SMTP password"
              type="password"
              :hint="
                smtpPasswordConfigured
                  ? 'A password is currently set — leave blank to keep it'
                  : 'No password currently set'
              "
              persistent-hint
              :error-messages="fieldErrors.SmtpPassword"
              class="mb-4"
            />
            <v-text-field
              id="settings-smtp-from-address"
              v-model="form.smtpFromAddress"
              label="SMTP from address"
              :error-messages="fieldErrors.SmtpFromAddress"
              class="mb-2"
            />
            <v-text-field
              id="settings-smtp-from-name"
              v-model="form.smtpFromName"
              label="SMTP from name"
              :error-messages="fieldErrors.SmtpFromName"
              class="mb-2"
            />
            <v-switch
              id="settings-smtp-use-ssl-on-connect"
              v-model="form.smtpUseSslOnConnect"
              label="Use SSL on connect"
              color="accent"
            />
          </v-card-text>
        </v-card>

        <v-btn id="settings-save" color="accent" block size="large" :loading="phase === 'saving'" @click="save">
          Save
        </v-btn>
      </template>

      <template v-else-if="phase === 'restarting'">
        <div class="d-flex flex-column align-center text-center py-16">
          <v-progress-circular indeterminate color="accent" size="64" class="mb-6" />
          <h2 class="text-h5 mb-2">Settings saved — restarting</h2>
          <p class="text-body-2">The app is applying your changes and will be back in a moment.</p>
        </div>
      </template>

      <template v-else-if="phase === 'restart-failed'">
        <v-alert type="warning" class="mb-4">
          Settings were saved, but the app hasn't come back up yet. If this is a Docker deployment,
          check that the container has a restart policy configured — otherwise, restart it manually.
          Then reload this page.
        </v-alert>
        <v-btn id="settings-reload" color="accent" block @click="reload">Reload</v-btn>
      </template>
    </div>
  </v-container>
</template>
