import { httpClient } from './httpClient'
import type { UserDto } from './types'

export const usersApi = {
  me: () => httpClient.get<UserDto>('/api/users/me'),
}
