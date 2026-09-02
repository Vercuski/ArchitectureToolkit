import { httpClient } from './httpClient'

export const accountApi = {
  // Anonymous endpoint — httpClient only attaches an Authorization header
  // when a session already exists, so this works unauthenticated as-is.
  setPassword: (email: string, token: string, newPassword: string, confirmPassword: string) =>
    httpClient.post<void>('/api/account/set-password', {
      Email: email,
      Token: token,
      NewPassword: newPassword,
      ConfirmPassword: confirmPassword,
    }),
}
