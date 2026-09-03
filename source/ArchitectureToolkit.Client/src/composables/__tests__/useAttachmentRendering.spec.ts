import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const downloadBlobMock = vi.fn()
vi.mock('@/api/attachments', () => ({
  attachmentsApi: { downloadBlob: downloadBlobMock },
}))

const { patchAttachmentImages, interceptAttachmentLinks } = await import('../useAttachmentRendering')

const PROJECT_ID = '11111111-1111-1111-1111-111111111111'
const ATTACHMENT_ID = '22222222-2222-2222-2222-222222222222'
const ATTACHMENT_URL = `/api/projects/${PROJECT_ID}/attachments/${ATTACHMENT_ID}/download`

describe('patchAttachmentImages', () => {
  let container: HTMLElement
  let createObjectURLMock: ReturnType<typeof vi.fn>

  beforeEach(() => {
    container = document.createElement('div')
    document.body.appendChild(container)
    downloadBlobMock.mockReset()
    createObjectURLMock = vi.fn().mockReturnValue('blob:mock-object-url')
    vi.stubGlobal('URL', { ...URL, createObjectURL: createObjectURLMock })
  })

  afterEach(() => {
    container.remove()
    vi.unstubAllGlobals()
  })

  it('swaps an <img> pointing at an attachment download URL for an authenticated blob URL', async () => {
    container.innerHTML = `<img src="${ATTACHMENT_URL}" />`
    const blob = new Blob(['image bytes'])
    downloadBlobMock.mockResolvedValue(blob)

    await patchAttachmentImages(container)

    expect(downloadBlobMock).toHaveBeenCalledWith(PROJECT_ID, ATTACHMENT_ID)
    const img = container.querySelector('img')!
    expect(img.src).toBe('blob:mock-object-url')
    expect(img.dataset.attachmentSwapped).toBe('true')
  })

  it('leaves an <img> whose src is not an attachment URL untouched', async () => {
    container.innerHTML = `<img src="https://example.com/photo.png" />`

    await patchAttachmentImages(container)

    expect(downloadBlobMock).not.toHaveBeenCalled()
    expect(container.querySelector('img')!.src).toBe('https://example.com/photo.png')
  })

  it('does not re-fetch an image already swapped on a previous call', async () => {
    container.innerHTML = `<img src="${ATTACHMENT_URL}" />`
    downloadBlobMock.mockResolvedValue(new Blob(['image bytes']))

    await patchAttachmentImages(container)
    await patchAttachmentImages(container)

    expect(downloadBlobMock).toHaveBeenCalledTimes(1)
  })

  it('leaves a failed image unswapped rather than throwing', async () => {
    container.innerHTML = `<img src="${ATTACHMENT_URL}" />`
    downloadBlobMock.mockRejectedValue(new Error('network error'))

    await expect(patchAttachmentImages(container)).resolves.toBeUndefined()
    expect(container.querySelector('img')!.dataset.attachmentSwapped).toBeUndefined()
  })
})

describe('interceptAttachmentLinks', () => {
  let container: HTMLElement
  let createObjectURLMock: ReturnType<typeof vi.fn>
  let revokeObjectURLMock: ReturnType<typeof vi.fn>

  beforeEach(() => {
    container = document.createElement('div')
    document.body.appendChild(container)
    downloadBlobMock.mockReset()
    createObjectURLMock = vi.fn().mockReturnValue('blob:mock-object-url')
    revokeObjectURLMock = vi.fn()
    vi.stubGlobal('URL', { ...URL, createObjectURL: createObjectURLMock, revokeObjectURL: revokeObjectURLMock })
    vi.useFakeTimers()
  })

  afterEach(() => {
    container.remove()
    vi.unstubAllGlobals()
    vi.useRealTimers()
  })

  it('intercepts a click on an attachment link and downloads it as a blob instead of navigating', async () => {
    container.innerHTML = `<a href="${ATTACHMENT_URL}">report.pdf</a>`
    const blob = new Blob(['file bytes'])
    downloadBlobMock.mockResolvedValue(blob)
    const cleanup = interceptAttachmentLinks(container)

    const anchor = container.querySelector('a')!
    const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true })
    const preventDefaultSpy = vi.spyOn(clickEvent, 'preventDefault')
    anchor.dispatchEvent(clickEvent)
    await vi.waitFor(() => expect(downloadBlobMock).toHaveBeenCalled())

    expect(preventDefaultSpy).toHaveBeenCalled()
    expect(downloadBlobMock).toHaveBeenCalledWith(PROJECT_ID, ATTACHMENT_ID)
    expect(createObjectURLMock).toHaveBeenCalledWith(blob)

    cleanup()
  })

  it('leaves a normal external link to navigate untouched', () => {
    container.innerHTML = `<a href="https://example.com">External</a>`
    const cleanup = interceptAttachmentLinks(container)

    const anchor = container.querySelector('a')!
    const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true })
    const preventDefaultSpy = vi.spyOn(clickEvent, 'preventDefault')
    anchor.dispatchEvent(clickEvent)

    expect(preventDefaultSpy).not.toHaveBeenCalled()
    expect(downloadBlobMock).not.toHaveBeenCalled()

    cleanup()
  })

  it('stops intercepting after cleanup is called', () => {
    container.innerHTML = `<a href="${ATTACHMENT_URL}">report.pdf</a>`
    const cleanup = interceptAttachmentLinks(container)
    cleanup()

    const anchor = container.querySelector('a')!
    const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true })
    const preventDefaultSpy = vi.spyOn(clickEvent, 'preventDefault')
    anchor.dispatchEvent(clickEvent)

    expect(preventDefaultSpy).not.toHaveBeenCalled()
    expect(downloadBlobMock).not.toHaveBeenCalled()
  })
})
