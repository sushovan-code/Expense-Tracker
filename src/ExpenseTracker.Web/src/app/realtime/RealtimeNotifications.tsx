import { formatCurrency, formatMonth } from '@/shared/lib/format'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import { isFromThisTab, type ChangeNotice } from '@/shared/realtime/connection'
import { useRealtimeEvent } from '@/shared/realtime/hooks'

const describeChange = (notice: ChangeNotice, noun: string) => {
  const count = notice.ids.length
  return `${count} ${noun}${count === 1 ? '' : 's'} ${notice.action}`
}

/** App-wide toasts for realtime events: changes from other sessions and budget overruns. */
export function RealtimeNotifications() {
  const toast = useToast()

  useRealtimeEvent('ExpensesChanged', (notice) => {
    if (isFromThisTab(notice)) return
    toast.info('Updated from another session', { description: describeChange(notice, 'expense') })
  })

  useRealtimeEvent('CategoriesChanged', (notice) => {
    if (isFromThisTab(notice)) return
    toast.info('Categories changed in another session', { description: describeChange(notice, 'category') })
  })

  useRealtimeEvent('BudgetExceeded', (alert) => {
    logger.event(
      'budget.exceeded_notified',
      `Budget exceeded for category ${alert.categoryId} (${alert.year}-${alert.month})`,
      {
        'category.id': alert.categoryId,
        'budget.amount': alert.budget,
        'budget.spent': alert.spent,
        'budget.period': `${alert.year}-${String(alert.month).padStart(2, '0')}`,
      },
      'warn',
    )
    toast.warning(`${alert.categoryName} is over budget`, {
      description: `${formatCurrency(alert.spent)} spent of ${formatCurrency(alert.budget)} in ${formatMonth({ year: alert.year, month: alert.month })}`,
      duration: 9000,
    })
  })

  return null
}
