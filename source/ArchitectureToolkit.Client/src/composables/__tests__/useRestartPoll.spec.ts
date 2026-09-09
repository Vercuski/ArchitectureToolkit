import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const statusMock = vi.fn()
vi.mock('@/api/setup', () => ({ setupApi: { status: statusMock } }))

const { pollUntilBackUp } = await import('../useRestartPoll')

describe('pollUntilBackUp', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    statusMock.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('calls onBackUp and resolves true once the API reports configured', async () => {
    statusMock.mockResolvedValue({ isConfigured: true })
    const onBackUp = vi.fn()

    const resultPromise = pollUntilBackUp(onBackUp)
    await vi.advanceTimersByTimeAsync(3000)

    expect(await resultPromise).toBe(true)
    expect(onBackUp).toHaveBeenCalledOnce()
  })

  it('keeps polling through connection errors (expected mid-restart) until the API reports configured', async () => {
    statusMock.mockRejectedValueOnce(new Error('connection refused'))
    statusMock.mockRejectedValueOnce(new Error('connection refused'))
    statusMock.mockResolvedValue({ isConfigured: true })
    const onBackUp = vi.fn()

    const resultPromise = pollUntilBackUp(onBackUp)
    await vi.advanceTimersByTimeAsync(3000 + 2000 + 2000)

    expect(await resultPromise).toBe(true)
    expect(statusMock).toHaveBeenCalledTimes(3)
  })

  it('does not call onBackUp while the API keeps reporting not-yet-configured', async () => {
    statusMock.mockResolvedValue({ isConfigured: false })
    const onBackUp = vi.fn()

    void pollUntilBackUp(onBackUp)
    await vi.advanceTimersByTimeAsync(3000 + 2000 + 2000)

    expect(onBackUp).not.toHaveBeenCalled()
  })

  it('resolves false without calling onBackUp once the ~2 minute poll budget is exhausted', async () => {
    statusMock.mockResolvedValue({ isConfigured: false })
    const onBackUp = vi.fn()

    const resultPromise = pollUntilBackUp(onBackUp)
    await vi.advanceTimersByTimeAsync(3000 + 60 * 2000)

    expect(await resultPromise).toBe(false)
    expect(onBackUp).not.toHaveBeenCalled()
  })
})
