import { useState } from 'react'
import { useBudgetOverview } from '@/features/budgets'
import { useExpenseSummary, useExpensesPage } from '@/features/expenses'
import { useErrorToast } from '@/shared/hooks/useErrorToast'
import { addMonths, currentMonth, formatMonth, monthRange, sameMonth } from '@/shared/lib/format'

export const TREND_MONTHS = 6

/** ViewModel for the dashboard: the selected month and everything derived from it. */
export function useDashboardViewModel() {
  const [month, setMonth] = useState(currentMonth)
  const { from, to } = monthRange(month)
  const trendFrom = monthRange(addMonths(month, -(TREND_MONTHS - 1))).from

  const summary = useExpenseSummary(from, to)
  const trendSummary = useExpenseSummary(trendFrom, to)
  const recent = useExpensesPage({ from, to, page: 1, pageSize: 5, sortBy: 'date', sortDir: 'desc' })
  const budgets = useBudgetOverview(month.year, month.month)

  const queries = [summary, trendSummary, recent, budgets]
  const error = queries.find((q) => q.error)?.error ?? null
  useErrorToast(error ?? undefined, "Couldn't load the dashboard")

  const monthPicker = {
    key: `${month.year}-${month.month}`,
    label: formatMonth(month),
    canGoNext: !sameMonth(month, currentMonth()),
    previous: () => setMonth((m) => addMonths(m, -1)),
    next: () => setMonth((m) => addMonths(m, 1)),
  }

  const status = {
    isLoading: queries.some((q) => !q.data),
    isRefreshing: queries.some((q) => q.isFetching),
    error,
    retry: () => queries.forEach((q) => void q.refetch()),
  }

  if (!summary.data || !trendSummary.data || !recent.data || !budgets.data) {
    return { month: monthPicker, status, data: null }
  }

  const trend = Array.from({ length: TREND_MONTHS }, (_, i) => {
    const ym = addMonths(month, i - (TREND_MONTHS - 1))
    const match = trendSummary.data.byMonth.find((m) => m.year === ym.year && m.month === ym.month)
    return { label: formatMonth(ym, 'short'), total: match?.total ?? 0, current: i === TREND_MONTHS - 1 }
  })
  const previousTotal = trend[TREND_MONTHS - 2].total

  return {
    month: monthPicker,
    status,
    data: {
      summary: summary.data,
      trend,
      changeVsPreviousMonth: previousTotal > 0 ? ((summary.data.total - previousTotal) / previousTotal) * 100 : null,
      topCategory: summary.data.byCategory[0],
      recent: recent.data.items,
      budgets: budgets.data,
    },
  }
}
