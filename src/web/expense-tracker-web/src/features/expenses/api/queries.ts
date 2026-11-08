import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { QueryRoots } from '@/shared/api/queryKeys'
import { useService } from '@/shared/di/services'
import type { IsoDate } from '@/shared/types'
import type { ExpenseInput, ExpenseQuery } from '../model/types'
import { ExpensesApiToken } from './expensesApi'

export const expenseKeys = {
  all: [QueryRoots.expenses] as const,
  list: (query: ExpenseQuery) => [QueryRoots.expenses, 'list', query] as const,
  summary: (from?: IsoDate, to?: IsoDate) => [QueryRoots.expenses, 'summary', from ?? null, to ?? null] as const,
}

/** A page of expenses. Keeps showing the previous page while the next one loads. */
export function useExpensesPage(query: ExpenseQuery) {
  const api = useService(ExpensesApiToken)
  return useQuery({
    queryKey: expenseKeys.list(query),
    queryFn: () => api.list(query),
    placeholderData: keepPreviousData,
  })
}

export function useExpenseSummary(from?: IsoDate, to?: IsoDate) {
  const api = useService(ExpensesApiToken)
  return useQuery({ queryKey: expenseKeys.summary(from, to), queryFn: () => api.summary(from, to), placeholderData: keepPreviousData })
}

/**
 * Expense commands. Each one refreshes every cache an expense change can affect: expense lists and
 * summaries, budget status and per-category counts.
 */
export function useExpenseMutations() {
  const api = useService(ExpensesApiToken)
  const queryClient = useQueryClient()
  const invalidate = () =>
    Promise.all(
      [QueryRoots.expenses, QueryRoots.budgets, QueryRoots.categories].map((root) =>
        queryClient.invalidateQueries({ queryKey: [root] }),
      ),
    )

  return {
    create: useMutation({ mutationFn: (input: ExpenseInput) => api.create(input), onSuccess: invalidate }),
    update: useMutation({
      mutationFn: ({ id, input }: { id: string; input: ExpenseInput }) => api.update(id, input),
      onSuccess: invalidate,
    }),
    remove: useMutation({ mutationFn: (id: string) => api.remove(id), onSuccess: invalidate }),
    bulkRemove: useMutation({ mutationFn: (ids: string[]) => api.bulkRemove(ids), onSuccess: invalidate }),
    exportCsv: useMutation({ mutationFn: api.exportCsv }),
  }
}
