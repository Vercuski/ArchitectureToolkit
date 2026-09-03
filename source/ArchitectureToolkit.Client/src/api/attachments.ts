import { httpClient } from './httpClient'
import type { DocumentAttachmentDto } from './types'

export const attachmentsApi = {
  upload: (projectId: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return httpClient.postForm<DocumentAttachmentDto>(`/api/projects/${projectId}/attachments`, formData)
  },

  /**
   * The URL embedded in markdown (image src / link href) — stable and
   * never expiring, unlike a signed URL would be, so it keeps working in
   * old document revisions indefinitely. It's never fetched directly by
   * the browser as a plain <img src>/<a href> though: neither can carry
   * the bearer token this endpoint requires, so rendering always goes
   * through downloadBlob below instead. See useAttachmentRendering.ts.
   */
  downloadUrl: (projectId: string, attachmentId: string) =>
    `/api/projects/${projectId}/attachments/${attachmentId}/download`,

  downloadBlob: (projectId: string, attachmentId: string) =>
    httpClient.getBlob(attachmentsApi.downloadUrl(projectId, attachmentId)),
}
