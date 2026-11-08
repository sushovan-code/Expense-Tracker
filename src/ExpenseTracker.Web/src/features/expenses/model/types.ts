import type { IsoDate, PagedResponse, SortDirection } from '@/shared/types'

export interface Expense {
  id: number
  description: string
  amount: number
  date: IsoDate
  categoryId: number
  categoryName: string
  categoryColor: string
  notes: string | null
  createdAt: string
}

export interface ExpenseInput {
  description: string
  amount: number
  date: IsoDate
  categoryId: number
  notes: string | null
}

export type ExpenseSortField = 'date' | 'amount' | 'description' | 'category' | 'createdat'

export interface ExpenseFilters {
  from?: IsoDate
  to?: IsoDate
  categoryId?: number
  search?: string
}

export interface ExpenseQuery extends ExpenseFilters {
  page?: number
  pageSize?: number
  sortBy?: ExpenseSortField
  sortDir?: SortDirection
}

/** A page of expenses plus the total amount of the whole filtered set. */
export interface ExpensePage extends PagedResponse<Expense> {
  totalAmount: number
}

export interface BulkDeleteResult {
  deleted: number
  deletedIds: number[]
}

export interface CategoryTotal {
  categoryId: number
  categoryName: string
  categoryColor: string
  total: number
  count: number
}

export interface MonthTotal {
  year: number
  month: number
  total: number
}

export interface ExpenseSummary {
  total: number
  count: number
  average: number
  byCategory: CategoryTotal[]
  byMonth: MonthTotal[]
}

/** Minimal category shape the expense screens need (dropdowns, filters). */
export interface CategoryOption {
  id: number
  name: string
  color: string
}
