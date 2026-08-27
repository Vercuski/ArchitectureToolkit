// Mirrors ArchitectureToolkit.Domain.ValueObjects.ProjectRole, serialized
// as its string name (Program.cs registers JsonStringEnumConverter).
export type ProjectRole = 'Viewer' | 'Editor' | 'Owner'

// Mirrors ArchitectureToolkit.Application.Contracts.Projects.ProjectDto
export interface ProjectDto {
  id: string
  name: string
}

// Mirrors ArchitectureToolkit.Application.Contracts.Projects.ProjectMemberDto
export interface ProjectMemberDto {
  projectId: string
  userId: string
  userName: string
  userEmail: string
  role: ProjectRole
}

// Mirrors ArchitectureToolkit.Application.Contracts.Users.UserDto
export interface UserDto {
  id: string
  name: string
  email: string
  systemRole: string
}
