import { useQueryClient } from '@tanstack/react-query'
import { QueryRoots, type QueryRoot } from '@/shared/api/queryKeys'
import { isFromThisTab, type ChangeNotice } from '@/shared/realtime/connection'
import { useRealtimeEvent } from '@/shared/realtime/hooks'

/**
 * Bridges SignalR to the query cache: when another session changes data, the affected caches are
 * invalidated and every mounted view refetches. Changes made by this tab are skipped — its own
 * mutations already invalidated them. After a reconnect everything is refreshed (events may be missed).
 */
export function RealtimeQuerySync() {
  const queryClient = useQueryClient()

  const invalidate = (notice: ChangeNotice, roots: QueryRoot[]) => {
    if (isFromThisTab(notice)) return
    for (const root of roots) void queryClient.invalidateQueries({ queryKey: [root] })
  }

  // An expense change affects lists, summaries, budgets and per-category counts.
  useRealtimeEvent('ExpensesChanged', (notice) =>
    invalidate(notice, [QueryRoots.expenses, QueryRoots.budgets, QueryRoots.categories]))
  // Category names/colors appear on expenses and budgets too.
  useRealtimeEvent('CategoriesChanged', (notice) =>
    invalidate(notice, [QueryRoots.categories, QueryRoots.expenses, QueryRoots.budgets]))
  useRealtimeEvent('Resynced', () => void queryClient.invalidateQueries())

  return null
}
