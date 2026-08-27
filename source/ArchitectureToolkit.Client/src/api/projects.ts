import { httpClient } from './httpClient'
import type { ProjectDto, ProjectMemberDto, ProjectRole } from './types'

export const projectsApi = {
  list: () => httpClient.get<ProjectDto[]>('/api/projects'),

  get: (id: string) => httpClient.get<ProjectDto>(`/api/projects/${id}`),

  create: (name: string) => httpClient.post<ProjectDto>('/api/projects', { Name: name }),

  listMembers: (projectId: string) =>
    httpClient.get<ProjectMemberDto[]>(`/api/projects/${projectId}/members`),

  addMember: (projectId: string, userId: string, role: ProjectRole) =>
    httpClient.post<ProjectMemberDto>(`/api/projects/${projectId}/members`, {
      UserId: userId,
      Role: role,
    }),

  updateMemberRole: (projectId: string, userId: string, role: ProjectRole) =>
    httpClient.put<ProjectMemberDto>(`/api/projects/${projectId}/members/${userId}`, {
      Role: role,
    }),

  removeMember: (projectId: string, userId: string) =>
    httpClient.delete<void>(`/api/projects/${projectId}/members/${userId}`),
}
