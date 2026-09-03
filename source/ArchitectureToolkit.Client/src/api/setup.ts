import { httpClient } from './httpClient'

export interface SetupStatus {
  isConfigured: boolean
}

// Property names deliberately match CompleteSetupRequest's C# names
// (PascalCase) rather than following this file's own camelCase
// convention — same pattern as api/account.ts's setPassword payload.
// ASP.NET Core's default JSON input binding is case-insensitive, but
// matching the backend's own names exactly keeps the two sides easy to
// diff against each other.
export interface CompleteSetupPayload {
  QueryDbConnection: string
  CommandDbConnection: string
  TemplateLibraryRootPath: string
  Authority: string | null
  ClientId: string
  Audience: string
  SmtpHost: string | null
  SmtpPort: number
  SmtpUsername: string | null
  SmtpPassword: string | null
  SmtpFromAddress: string
  SmtpFromName: string
  SmtpUseSslOnConnect: boolean
  InitialUserEmail: string
  InitialUserPassword: string
  InitialUserConfirmPassword: string
}

// Mirrors ArchitectureToolkit.Presentation.API.Setup.SetupCompletionError
export interface SetupFieldError {
  field: string
  message: string
}

export const setupApi = {
  status: () => httpClient.get<SetupStatus>('/api/setup/status'),
  complete: (payload: CompleteSetupPayload) => httpClient.post<void>('/api/setup/complete', payload),
}
