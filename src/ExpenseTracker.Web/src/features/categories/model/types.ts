import type { SortDirection } from '@/shared/types'

export interface Category {
  id: number
  name: string
  color: string
  monthlyBudget: number | null
  expenseCount: number
}

/** Minimal category shape for dropdowns and filters. */
export interface CategoryLookup {
  id: number
  name: string
  color: string
}

export interface CategoryInput {
  name: string
  color: string
  monthlyBudget: number | null
}

export type CategorySortField = 'name' | 'expenses' | 'budget'

export interface CategoryQuery {
  search?: string
  page?: number
  pageSize?: number
  sortBy?: CategorySortField
  sortDir?: SortDirection
}
