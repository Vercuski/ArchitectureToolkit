import { httpClient } from './httpClient'
import type {
  BumpType,
  DocumentRevisionDetailDto,
  DocumentRevisionDto,
  ProjectDocumentDetailDto,
  ProjectDocumentSummaryDto,
} from './types'

export const documentsApi = {
  listForProject: (projectId: string) =>
    httpClient.get<ProjectDocumentSummaryDto[]>(`/api/projects/${projectId}/documents`),

  create: (
    projectId: string,
    categoryId: string,
    title: string,
    content: string,
    sourceTemplateRevisionId: string | null,
  ) =>
    httpClient.post<ProjectDocumentDetailDto>(`/api/projects/${projectId}/documents`, {
      CategoryId: categoryId,
      Title: title,
      SourceTemplateRevisionId: sourceTemplateRevisionId,
      Content: content,
    }),

  get: (id: string) => httpClient.get<ProjectDocumentDetailDto>(`/api/documents/${id}`),

  listRevisions: (id: string) => httpClient.get<DocumentRevisionDto[]>(`/api/documents/${id}/revisions`),

  getRevision: (id: string, revisionId: string) =>
    httpClient.get<DocumentRevisionDetailDto>(`/api/documents/${id}/revisions/${revisionId}`),

  createRevision: (id: string, expectedCurrentRevisionId: string, bumpType: BumpType, content: string) =>
    httpClient.post<DocumentRevisionDto>(`/api/documents/${id}/revisions`, {
      ExpectedCurrentRevisionId: expectedCurrentRevisionId,
      BumpType: bumpType,
      Content: content,
    }),
}
