import { httpClient } from './httpClient'
import type { CreateUserResult, SystemRole, UserDto, UserManagementDto } from './types'

export const usersApi = {
  me: () => httpClient.get<UserDto>('/api/users/me'),

  // User Management tab (ADR-0017) — architect-only; server sorts by
  // email, so the returned order is already what the tab should display.
  list: () => httpClient.get<UserManagementDto[]>('/api/users'),

  setActive: (userId: string, isActive: boolean) =>
    httpClient.put<UserManagementDto>(`/api/users/${userId}/active`, { IsActive: isActive }),

  // Admin-provisions a new user via invite link (ADR-0018) —
  // architect-only, self-hosted deployments only.
  create: (email: string, systemRole: SystemRole) =>
    httpClient.post<CreateUserResult>('/api/users', { Email: email, SystemRole: systemRole }),
}
