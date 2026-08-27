import { httpClient } from './httpClient'
import type {
  BumpType,
  TemplateDetailDto,
  TemplateRevisionDetailDto,
  TemplateRevisionDto,
  TemplateSummaryDto,
} from './types'

export const templatesApi = {
  list: () => httpClient.get<TemplateSummaryDto[]>('/api/templates'),

  get: (id: string) => httpClient.get<TemplateDetailDto>(`/api/templates/${id}`),

  create: (categoryId: string, name: string, content: string) =>
    httpClient.post<TemplateDetailDto>('/api/templates', {
      CategoryId: categoryId,
      Name: name,
      Content: content,
    }),

  listRevisions: (templateId: string) =>
    httpClient.get<TemplateRevisionDto[]>(`/api/templates/${templateId}/revisions`),

  getRevision: (templateId: string, revisionId: string) =>
    httpClient.get<TemplateRevisionDetailDto>(`/api/templates/${templateId}/revisions/${revisionId}`),

  createRevision: (
    templateId: string,
    expectedCurrentRevisionId: string,
    bumpType: BumpType,
    content: string,
  ) =>
    httpClient.post<TemplateRevisionDto>(`/api/templates/${templateId}/revisions`, {
      ExpectedCurrentRevisionId: expectedCurrentRevisionId,
      BumpType: bumpType,
      Content: content,
    }),
}
