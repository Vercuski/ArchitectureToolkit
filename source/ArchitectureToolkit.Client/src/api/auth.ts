import { httpClient } from './httpClient'
import type { AuthConfigDto } from './types'

export const authApi = {
  getConfig: () => httpClient.get<AuthConfigDto>('/api/auth/config'),
}
