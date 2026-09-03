import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const getUserMock = vi.fn()

vi.mock('@/auth/oidcConfig', () => ({
  userManager: {
    getUser: getUserMock,
  },
}))

// Imported after the mock above so httpClient picks up the mocked userManager.
const { httpClient, ApiError } = await import('../httpClient')

function jsonResponse(body: unknown, init: ResponseInit = {}) {
  return new Response(JSON.stringify(body), {
    status: 200,
    headers: { 'content-type': 'application/json' },
    ...init,
  })
}

function headersFromLastCall(callIndex = 0) {
  const call = vi.mocked(fetch).mock.calls[callIndex]
  return new Headers(call?.[1]?.headers)
}

describe('httpClient', () => {
  beforeEach(() => {
    getUserMock.mockReset()
    vi.stubGlobal('fetch', vi.fn())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('attaches a bearer token when a signed-in user has an access token', async () => {
    getUserMock.mockResolvedValue({ access_token: 'token-123' })
    vi.mocked(fetch).mockResolvedValue(jsonResponse({ ok: true }))

    await httpClient.get('/api/projects')

    expect(headersFromLastCall().get('Authorization')).toBe('Bearer token-123')
  })

  it('omits the Authorization header when there is no signed-in user', async () => {
    getUserMock.mockResolvedValue(null)
    vi.mocked(fetch).mockResolvedValue(jsonResponse({ ok: true }))

    await httpClient.get('/api/projects')

    expect(headersFromLastCall().has('Authorization')).toBe(false)
  })

  it('sets Content-Type only when a request body is present', async () => {
    getUserMock.mockResolvedValue(null)
    // A fresh Response per call — reusing one Response across two request()
    // calls would fail on the second .json() read (bodies are single-use).
    vi.mocked(fetch).mockImplementation(async () => jsonResponse({}))

    await httpClient.get('/api/projects')
    expect(headersFromLastCall(0).has('Content-Type')).toBe(false)

    await httpClient.post('/api/projects', { name: 'Test' })
    expect(headersFromLastCall(1).get('Content-Type')).toBe('application/json')
  })

  it('returns undefined for a 204 No Content response', async () => {
    getUserMock.mockResolvedValue(null)
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 204 }))

    const result = await httpClient.delete('/api/projects/1')

    expect(result).toBeUndefined()
  })

  it('parses and returns a JSON body on success', async () => {
    getUserMock.mockResolvedValue(null)
    vi.mocked(fetch).mockResolvedValue(jsonResponse({ id: '1', name: 'Test' }))

    const result = await httpClient.get<{ id: string; name: string }>('/api/projects/1')

    expect(result).toEqual({ id: '1', name: 'Test' })
  })

  it('throws an ApiError carrying the status and parsed body on a non-ok response', async () => {
    getUserMock.mockResolvedValue(null)
    vi.mocked(fetch).mockResolvedValue(jsonResponse({ error: 'Conflict' }, { status: 409 }))

    const failure = httpClient.post('/api/documents/1/revisions', {})

    await expect(failure).rejects.toBeInstanceOf(ApiError)
    await expect(failure.catch((e) => e)).resolves.toMatchObject({
      status: 409,
      body: { error: 'Conflict' },
    })
  })

  it('does not attempt to parse a non-JSON error body', async () => {
    getUserMock.mockResolvedValue(null)
    vi.mocked(fetch).mockResolvedValue(new Response('Internal Server Error', { status: 500 }))

    await expect(httpClient.get('/api/projects').catch((e) => e)).resolves.toMatchObject({
      status: 500,
      body: undefined,
    })
  })

  it('sends a FormData body without setting Content-Type, letting the browser set the multipart boundary', async () => {
    getUserMock.mockResolvedValue(null)
    vi.mocked(fetch).mockResolvedValue(jsonResponse({ id: '1' }))

    const formData = new FormData()
    formData.append('file', new Blob(['content']), 'test.txt')
    await httpClient.postForm('/api/projects/1/attachments', formData)

    const call = vi.mocked(fetch).mock.calls[0]
    expect(call?.[1]?.body).toBe(formData)
    expect(headersFromLastCall().has('Content-Type')).toBe(false)
  })

  it('getBlob attaches the bearer token and returns the response body as a Blob', async () => {
    getUserMock.mockResolvedValue({ access_token: 'token-123' })
    vi.mocked(fetch).mockResolvedValue(new Response('file bytes', { status: 200 }))

    const blob = await httpClient.getBlob('/api/projects/1/attachments/2/download')

    expect(headersFromLastCall().get('Authorization')).toBe('Bearer token-123')
    expect(await blob.text()).toBe('file bytes')
  })

  it('getBlob throws an ApiError on a non-ok response', async () => {
    getUserMock.mockResolvedValue(null)
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 404 }))

    await expect(httpClient.getBlob('/api/projects/1/attachments/2/download').catch((e) => e)).resolves.toMatchObject({
      status: 404,
    })
  })
})
