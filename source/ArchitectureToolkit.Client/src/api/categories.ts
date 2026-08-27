import { httpClient } from './httpClient'
import type { CategoryDto } from './types'

export const categoriesApi = {
  list: () => httpClient.get<CategoryDto[]>('/api/categories'),
}
