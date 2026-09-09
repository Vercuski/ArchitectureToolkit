import { setupApi } from '@/api/setup'

const POLL_INTERVAL_MS = 2000
const POLL_INITIAL_DELAY_MS = 3000
const MAX_POLL_ATTEMPTS = 60 // ~2 minutes total, generous for a cold container pull/start

function delay(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms))
}

/**
 * Polls GET /api/setup/status until the API process comes back up after
 * something (SetupCompletionService, SettingsService) deliberately stops
 * it to apply newly-saved configuration. The caller has nothing else to
 * wait on for that restart — the HTTP response for the save itself
 * completes before the process actually stops (see either service's own
 * doc comment on why the restart is fire-and-forget).
 *
 * Extracted out of SetupWizardView rather than duplicated in SettingsView:
 * both need identical "wait for it to come back, then do X" behavior, and
 * SetupWizardView's own local copy of this was already exactly this shape
 * — a plain async function with no component-local state of its own, so
 * lifting it out costs nothing and removes a second, drift-prone copy.
 *
 * Calls onBackUp() once the API reports isConfigured again and resolves
 * true. Resolves false (without calling onBackUp) if the ~2 minute budget
 * is exhausted first — the caller decides how to present that, since
 * SetupWizardView and SettingsView show different fallback wording for
 * the same underlying "didn't come back in time" case.
 */
export async function pollUntilBackUp(onBackUp: () => void | Promise<void>): Promise<boolean> {
  await delay(POLL_INITIAL_DELAY_MS)

  for (let attempt = 0; attempt < MAX_POLL_ATTEMPTS; attempt++) {
    try {
      const status = await setupApi.status()
      if (status.isConfigured) {
        await onBackUp()
        return true
      }
    } catch {
      // Expected mid-restart (connection refused/reset) — just keep polling.
    }
    await delay(POLL_INTERVAL_MS)
  }

  return false
}
