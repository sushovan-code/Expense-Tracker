import { useQuery } from '@tanstack/react-query'
import type { HttpClient } from '@/shared/api/httpClient'
import { QueryRoots } from '@/shared/api/queryKeys'
import { createServiceToken, useService } from '@/shared/di/services'
import type { BudgetOverview } from '../model/types'

export interface BudgetsApi {
  overview(year: number, month: number): Promise<BudgetOverview>
}

export const BudgetsApiToken = createServiceToken<BudgetsApi>('BudgetsApi')

export function createBudgetsApi(http: HttpClient): BudgetsApi {
  return { overview: (year, month) => http.get('/budgets', { year, month }) }
}

export const budgetKeys = {
  all: [QueryRoots.budgets] as const,
  overview: (year: number, month: number) => [QueryRoots.budgets, 'overview', year, month] as const,
}

export function useBudgetOverview(year: number, month: number) {
  const api = useService(BudgetsApiToken)
  return useQuery({ queryKey: budgetKeys.overview(year, month), queryFn: () => api.overview(year, month) })
}
