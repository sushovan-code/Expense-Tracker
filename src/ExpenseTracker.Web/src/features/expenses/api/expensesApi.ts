import type { HttpClient } from '@/shared/api/httpClient'
import { createServiceToken } from '@/shared/di/services'
import type { IsoDate } from '@/shared/types'
import type { BulkDeleteResult, Expense, ExpenseInput, ExpensePage, ExpenseQuery, ExpenseSummary } from '../model/types'

/** Expense endpoints. View-models depend on this interface (resolved via DI), not on HTTP details. */
export interface ExpensesApi {
  list(query: ExpenseQuery): Promise<ExpensePage>
  summary(from?: IsoDate, to?: IsoDate): Promise<ExpenseSummary>
  create(input: ExpenseInput): Promise<Expense>
  update(id: number, input: ExpenseInput): Promise<Expense>
  remove(id: number): Promise<void>
  bulkRemove(ids: number[]): Promise<BulkDeleteResult>
  /** Downloads a CSV of the filtered, sorted expenses; returns the saved file name. */
  exportCsv(query: Omit<ExpenseQuery, 'page' | 'pageSize'>): Promise<string>
}

export const ExpensesApiToken = createServiceToken<ExpensesApi>('ExpensesApi')

export function createExpensesApi(http: HttpClient): ExpensesApi {
  return {
    list: (query) => http.get('/expenses', { ...query }),
    summary: (from, to) => http.get('/expenses/summary', { from, to }),
    create: (input) => http.post('/expenses', input),
    update: (id, input) => http.put(`/expenses/${id}`, input),
    remove: (id) => http.delete(`/expenses/${id}`),
    bulkRemove: (ids) => http.post('/expenses/bulk-delete', { ids }),
    exportCsv: (query) => http.download('/expenses/export', { ...query }, 'expenses.csv'),
  }
}
