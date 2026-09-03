import { attachmentsApi } from '@/api/attachments'

// Matches the path attachmentsApi.downloadUrl builds, wherever it appears
// in an href/src — as the relative string markdown actually stores, or as
// the browser's own absolute resolution of it (DOM .src/.href properties
// resolve relative attribute values against the document's base URL
// automatically) — either way the same regex finds it.
const ATTACHMENT_URL_PATTERN =
  /\/api\/projects\/([0-9a-fA-F-]{36})\/attachments\/([0-9a-fA-F-]{36})\/download/

interface ParsedAttachmentUrl {
  projectId: string
  attachmentId: string
}

function parseAttachmentUrl(url: string): ParsedAttachmentUrl | null {
  const match = ATTACHMENT_URL_PATTERN.exec(url)
  if (!match) {
    return null
  }
  return { projectId: match[1]!, attachmentId: match[2]! }
}

function triggerBlobDownload(blob: Blob, fileName: string) {
  const objectUrl = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = objectUrl
  anchor.download = fileName
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  // Deferred, not immediate: revoking synchronously right after click()
  // can race the browser's own read of the object URL for the download in
  // some engines — a short delay costs nothing here and avoids that.
  setTimeout(() => URL.revokeObjectURL(objectUrl), 1000)
}

/**
 * Finds every <img> inside container whose src is an attachment download
 * URL and swaps it for an authenticated blob URL — a plain <img src> can't
 * carry the bearer token that endpoint requires, so without this, every
 * uploaded image would just render broken. Safe to call repeatedly on the
 * same container (e.g. after every markdown re-render): already-swapped
 * images are skipped via a data attribute marker, and swapping is
 * idempotent regardless.
 */
export async function patchAttachmentImages(container: HTMLElement): Promise<void> {
  const images = container.querySelectorAll<HTMLImageElement>('img[src]')

  await Promise.all(
    [...images].map(async (img) => {
      if (img.dataset.attachmentSwapped === 'true') {
        return
      }

      const raw = img.getAttribute('src')
      const parsed = raw ? parseAttachmentUrl(raw) : null
      if (!parsed) {
        return
      }

      try {
        const blob = await attachmentsApi.downloadBlob(parsed.projectId, parsed.attachmentId)
        img.src = URL.createObjectURL(blob)
        img.dataset.attachmentSwapped = 'true'
      } catch {
        // Left as-is (a broken image, same as any other dead <img src>) —
        // not fatal to the rest of the document, and the underlying fetch
        // failure isn't actionable from here (wrong project, deleted
        // attachment, network hiccup all look the same at this layer).
      }
    }),
  )
}

/**
 * Delegates clicks within container: any <a> whose href is an attachment
 * download URL is intercepted and downloaded via an authenticated blob
 * fetch instead of navigating — same reasoning as patchAttachmentImages,
 * a plain <a href> can't carry a bearer token either, so an un-intercepted
 * click would just hit an unauthenticated 401. Every other link (a real
 * external URL the author typed) falls through to normal navigation
 * untouched. Returns a cleanup function to remove the listener.
 */
export function interceptAttachmentLinks(container: HTMLElement): () => void {
  const handler = (event: MouseEvent) => {
    const anchor = (event.target as HTMLElement | null)?.closest('a')
    const raw = anchor?.getAttribute('href')
    const parsed = raw ? parseAttachmentUrl(raw) : null
    if (!anchor || !parsed) {
      return
    }

    event.preventDefault()
    const fileName = anchor.textContent?.trim() || 'download'

    attachmentsApi
      .downloadBlob(parsed.projectId, parsed.attachmentId)
      .then((blob) => triggerBlobDownload(blob, fileName))
      .catch(() => {
        // Swallowed for the same reason as patchAttachmentImages' catch —
        // nothing actionable to do differently from here.
      })
  }

  container.addEventListener('click', handler)
  return () => container.removeEventListener('click', handler)
}
