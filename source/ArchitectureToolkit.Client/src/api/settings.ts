import { httpClient } from './httpClient'
import type { SettingsDto, UpdateSettingsPayload } from './types'

export const settingsApi = {
  get: () => httpClient.get<SettingsDto>('/api/settings'),
  update: (payload: UpdateSettingsPayload) => httpClient.put<void>('/api/settings', payload),
}
