import { userManager } from '@/auth/oidcConfig'

// Same base-URL reasoning as oidcConfig.ts: same-origin in production
// (ADR-0005), direct-to-API via VITE_API_BASE_URL in local dev (backed by
// the API's dev-only CORS policy).
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || window.location.origin

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly body: unknown,
  ) {
    super(`API request failed with status ${status}`)
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const user = await userManager.getUser()

  const headers = new Headers(init?.headers)
  headers.set('Accept', 'application/json')
  if (init?.body) {
    headers.set('Content-Type', 'application/json')
  }
  if (user?.access_token) {
    headers.set('Authorization', `Bearer ${user.access_token}`)
  }

  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers })

  if (response.status === 204) {
    return undefined as T
  }

  const contentType = response.headers.get('content-type') ?? ''
  const payload = contentType.includes('application/json') ? await response.json() : undefined

  if (!response.ok) {
    throw new ApiError(response.status, payload)
  }

  return payload as T
}

export const httpClient = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body !== undefined ? JSON.stringify(body) : undefined }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PUT', body: body !== undefined ? JSON.stringify(body) : undefined }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}
