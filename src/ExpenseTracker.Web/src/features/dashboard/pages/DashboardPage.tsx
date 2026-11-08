import { Link } from 'react-router-dom'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { BudgetsCard } from '@/features/budgets'
import { ExpenseList } from '@/features/expenses'
import { Permission } from '@/shared/auth/permissions'
import { useAccess } from '@/shared/auth/useSession'
import { formatCompactCurrency, formatCurrency } from '@/shared/lib/format'
import { staggerIndex } from '@/shared/lib/motion'
import { AnimatedCurrency, AnimatedNumber } from '@/shared/ui/AnimatedCurrency'
import { BarChartIcon, ChevronLeftIcon, ChevronRightIcon, ReceiptIcon, TagIcon, WalletIcon } from '@/shared/ui/Icons'
import { StatCard } from '@/shared/ui/StatCard'
import { EmptyState, ErrorState, LoadingState } from '@/shared/ui/States'
import { TREND_MONTHS, useDashboardViewModel } from '../viewModels/useDashboardViewModel'

const tooltipStyles = {
  contentStyle: {
    background: 'var(--surface-elevated)',
    border: '1px solid var(--glass-border)',
    borderRadius: 12,
    boxShadow: 'var(--shadow-lg)',
    backdropFilter: 'blur(12px)',
    color: 'var(--text)',
  },
  labelStyle: { color: 'var(--text-muted)' },
  itemStyle: { color: 'var(--text)' },
}

/** View: the dashboard, bound to its ViewModel. */
export function DashboardPage() {
  const { month, status, data } = useDashboardViewModel()
  const canAddExpense = useAccess().can(Permission.ExpenseAdd)

  const header = (
    <div className="page-header">
      <div>
        <h1 className="page-title">Dashboard</h1>
        <p className="page-subtitle">Your spending at a glance</p>
      </div>
      <div className="month-picker glass">
        <button type="button" className="icon-button" onClick={month.previous} aria-label="Previous month">
          <ChevronLeftIcon />
        </button>
        <span key={month.key} className="month-picker__label" aria-live="polite">
          {month.label}
        </span>
        <button type="button" className="icon-button" onClick={month.next} aria-label="Next month" disabled={!month.canGoNext}>
          <ChevronRightIcon />
        </button>
      </div>
    </div>
  )

  if (status.error && !data) {
    return (
      <>
        {header}
        <ErrorState error={status.error} onRetry={status.retry} />
      </>
    )
  }
  if (!data) {
    return (
      <>
        {header}
        <LoadingState />
      </>
    )
  }

  const { summary, trend, changeVsPreviousMonth: change, topCategory, recent, budgets } = data

  return (
    <div className={status.isRefreshing ? 'is-refreshing' : undefined}>
      {header}

      <section className="stats-grid stagger" aria-label="Summary">
        <StatCard
          style={staggerIndex(0)}
          icon={<WalletIcon />}
          tint="#6366f1"
          label="Total spent"
          value={<AnimatedCurrency value={summary.total} />}
          hint={
            change === null ? (
              'No spending last month'
            ) : (
              <span className={change > 0 ? 'trend trend--up' : 'trend trend--down'}>
                {change > 0 ? '▲' : '▼'} {Math.abs(change).toFixed(0)}% vs last month
              </span>
            )
          }
        />
        <StatCard
          style={staggerIndex(1)}
          icon={<ReceiptIcon />}
          tint="#ec4899"
          label="Transactions"
          value={<AnimatedNumber value={summary.count} />}
          hint={`in ${month.label}`}
        />
        <StatCard
          style={staggerIndex(2)}
          icon={<BarChartIcon />}
          tint="#06b6d4"
          label="Average expense"
          value={<AnimatedCurrency value={summary.average} />}
          hint="per transaction"
        />
        <StatCard
          style={staggerIndex(3)}
          icon={<TagIcon />}
          tint={topCategory?.categoryColor ?? '#f59e0b'}
          label="Top category"
          value={topCategory?.categoryName ?? '—'}
          hint={topCategory ? formatCurrency(topCategory.total) : 'No expenses yet'}
        />
      </section>

      <section className="charts-grid stagger">
        <div className="card" style={staggerIndex(4)}>
          <h2 className="card__title">Spending by category</h2>
          {summary.byCategory.length === 0 ? (
            <EmptyState title="No expenses this month" />
          ) : (
            <div className="donut">
              <div className="donut__chart">
                <ResponsiveContainer width="100%" height={220}>
                  <PieChart>
                    <Pie
                      data={summary.byCategory}
                      dataKey="total"
                      nameKey="categoryName"
                      innerRadius="64%"
                      outerRadius="92%"
                      paddingAngle={3}
                      cornerRadius={6}
                      animationDuration={900}
                      animationEasing="ease-out"
                    >
                      {summary.byCategory.map((c) => (
                        <Cell key={c.categoryId} fill={c.categoryColor} />
                      ))}
                    </Pie>
                    <Tooltip formatter={(value) => formatCurrency(Number(value))} {...tooltipStyles} />
                  </PieChart>
                </ResponsiveContainer>
                <div className="donut__center">
                  <span className="donut__center-label">Total</span>
                  <span className="donut__center-value">{formatCompactCurrency(summary.total)}</span>
                </div>
              </div>
              <ul className="legend">
                {summary.byCategory.map((c) => (
                  <li key={c.categoryId} className="legend__item">
                    <span className="legend__swatch" style={{ background: c.categoryColor }} />
                    <span className="legend__name">{c.categoryName}</span>
                    <span className="legend__value">{formatCurrency(c.total)}</span>
                    <span className="legend__pct">{((c.total / summary.total) * 100).toFixed(0)}%</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>

        <div className="card" style={staggerIndex(5)}>
          <h2 className="card__title">Last {TREND_MONTHS} months</h2>
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={trend} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
              <defs>
                <linearGradient id="bar-current" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#a855f7" />
                  <stop offset="100%" stopColor="#6366f1" />
                </linearGradient>
              </defs>
              <CartesianGrid vertical={false} strokeDasharray="3 3" />
              <XAxis dataKey="label" tickLine={false} axisLine={false} />
              <YAxis tickFormatter={formatCompactCurrency} tickLine={false} axisLine={false} width={64} />
              <Tooltip formatter={(value) => [formatCurrency(Number(value)), 'Spent']} {...tooltipStyles} />
              <Bar
                dataKey="total"
                radius={[8, 8, 0, 0]}
                maxBarSize={40}
                animationDuration={900}
                animationEasing="ease-out"
              >
                {trend.map((t) => (
                  <Cell
                    key={t.label}
                    className={t.current ? undefined : 'bar'}
                    fill={t.current ? 'url(#bar-current)' : undefined}
                  />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      </section>

      <div className="split-grid">
      <BudgetsCard overview={budgets} style={staggerIndex(6)} />
      <section className="card stagger-item" style={staggerIndex(7)}>
        <div className="card__header">
          <h2 className="card__title">Recent expenses</h2>
          <Link to="/expenses" className="link">
            View all →
          </Link>
        </div>
        {recent.length === 0 ? (
          <EmptyState title="Nothing recorded this month">
            {canAddExpense && (
              <Link to="/expenses" className="button button--primary">
                Add an expense
              </Link>
            )}
          </EmptyState>
        ) : (
          <ExpenseList expenses={recent} />
        )}
      </section>
      </div>
    </div>
  )
}
