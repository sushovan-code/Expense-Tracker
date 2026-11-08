/** ISO date string, `YYYY-MM-DD` (maps to DateOnly on the API). */
export type IsoDate = string

export type SortDirection = 'asc' | 'desc'

/** One page of results from a paged list endpoint. */
export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
