import type { HttpClient } from '@/shared/api/httpClient'
import { createServiceToken } from '@/shared/di/services'
import type { PagedResponse } from '@/shared/types'
import type { Category, CategoryInput, CategoryLookup, CategoryQuery } from '../model/types'

export interface CategoriesApi {
  list(query: CategoryQuery): Promise<PagedResponse<Category>>
  /** Every category (id, name, color) for dropdowns and filters. */
  lookup(): Promise<CategoryLookup[]>
  create(input: CategoryInput): Promise<Category>
  update(id: string, input: CategoryInput): Promise<Category>
  remove(id: string): Promise<void>
}

export const CategoriesApiToken = createServiceToken<CategoriesApi>('CategoriesApi')

export function createCategoriesApi(http: HttpClient): CategoriesApi {
  return {
    list: (query) => http.get('/categories', { ...query }),
    lookup: () => http.get('/categories/lookup'),
    create: (input) => http.post('/categories', input),
    update: (id, input) => http.put(`/categories/${id}`, input),
    remove: (id) => http.delete(`/categories/${id}`),
  }
}
