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

// Mirrors ArchitectureToolkit.Domain.ValueObjects.SystemRole, serialized
// as a string (JsonStringEnumConverter, see Program.cs).
export type SystemRole = 'Contributor' | 'Architect'

// Mirrors ArchitectureToolkit.Application.Contracts.Users.UserDto
export interface UserDto {
  id: string
  name: string
  email: string
  systemRole: string
}

// Mirrors ArchitectureToolkit.Application.Contracts.Users.UserManagementDto —
// the User Management tab's row shape (ADR-0017), narrower than UserDto.
export interface UserManagementDto {
  id: string
  email: string
  isActive: boolean
}

// Mirrors ArchitectureToolkit.Application.Contracts.Users.CreateUserResult
// (ADR-0018) — the created row plus how the invite went.
export interface CreateUserResult {
  user: UserManagementDto
  emailSent: boolean
  inviteLink: string | null
}

// Mirrors AuthController.GetConfig's anonymous response shape (ADR-0018).
export interface AuthConfigDto {
  useSelfHostedProvider: boolean
}

// Mirrors ArchitectureToolkit.Application.Contracts.Categories.CategoryDto
export interface CategoryDto {
  id: string
  code: string
  name: string
}

// Mirrors ArchitectureToolkit.Domain.ValueObjects.BumpType
export type BumpType = 'Major' | 'Minor' | 'Patch'

// Mirrors ArchitectureToolkit.Application.Contracts.Templates.TemplateSummaryDto
export interface TemplateSummaryDto {
  id: string
  categoryId: string
  name: string
  currentVersion: string
}

// Mirrors ArchitectureToolkit.Application.Contracts.Templates.TemplateDetailDto
export interface TemplateDetailDto {
  id: string
  categoryId: string
  name: string
  currentVersion: string
  currentRevisionId: string
  content: string
}

// Mirrors ArchitectureToolkit.Application.Contracts.Templates.TemplateRevisionDto
export interface TemplateRevisionDto {
  id: string
  templateId: string
  version: string
  bumpType: BumpType | null
  authorId: string
  createdAt: string
}

// Mirrors ArchitectureToolkit.Application.Contracts.Templates.TemplateRevisionDetailDto
export interface TemplateRevisionDetailDto extends TemplateRevisionDto {
  content: string
}

// Mirrors ArchitectureToolkit.Application.Contracts.ProjectDocuments.ProjectDocumentSummaryDto
export interface ProjectDocumentSummaryDto {
  id: string
  projectId: string
  categoryId: string
  title: string
  currentVersion: string
}

// Mirrors ArchitectureToolkit.Application.Contracts.ProjectDocuments.ProjectDocumentDetailDto
export interface ProjectDocumentDetailDto {
  id: string
  projectId: string
  categoryId: string
  title: string
  currentVersion: string
  currentRevisionId: string
  sourceTemplateRevisionId: string | null
  content: string
}

// Mirrors ArchitectureToolkit.Application.Contracts.ProjectDocuments.DocumentRevisionDto
export interface DocumentRevisionDto {
  id: string
  documentId: string
  version: string
  bumpType: BumpType | null
  authorId: string
  createdAt: string
}

// Mirrors ArchitectureToolkit.Application.Contracts.ProjectDocuments.DocumentRevisionDetailDto
export interface DocumentRevisionDetailDto extends DocumentRevisionDto {
  content: string
}
