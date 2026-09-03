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

async function buildAuthHeaders(extra?: HeadersInit): Promise<Headers> {
  const user = await userManager.getUser()
  const headers = new Headers(extra)
  if (user?.access_token) {
    headers.set('Authorization', `Bearer ${user.access_token}`)
  }
  return headers
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = await buildAuthHeaders(init?.headers)
  headers.set('Accept', 'application/json')
  // FormData bodies (attachment uploads) must NOT get an explicit
  // Content-Type here — the browser sets multipart/form-data with the
  // correct boundary itself only when Content-Type is left unset.
  if (init?.body && !(init.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
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

/**
 * Fetches a non-JSON body (an attachment's raw bytes) with the same
 * Authorization header every other request gets, and returns it as a
 * Blob. Used by anything rendering or downloading an attachment — a
 * plain <img src>/<a href> can't carry a bearer token, so both the
 * viewer's authenticated-image swap and its attachment-link download
 * route through this instead. Throws ApiError on a non-2xx response,
 * same as request().
 */
async function getBlob(path: string): Promise<Blob> {
  const headers = await buildAuthHeaders()
  const response = await fetch(`${API_BASE_URL}${path}`, { headers })

  if (!response.ok) {
    throw new ApiError(response.status, undefined)
  }

  return response.blob()
}

export const httpClient = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body !== undefined ? JSON.stringify(body) : undefined }),
  postForm: <T>(path: string, formData: FormData) => request<T>(path, { method: 'POST', body: formData }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PUT', body: body !== undefined ? JSON.stringify(body) : undefined }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
  getBlob,
}
