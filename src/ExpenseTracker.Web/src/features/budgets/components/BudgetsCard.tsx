import type { CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import { formatCurrency } from '@/shared/lib/format'
import type { BudgetOverview, BudgetState } from '../model/types'
import { EmptyState } from '@/shared/ui/States'

const STATE_LABEL: Record<BudgetState, string> = {
  ok: 'On track',
  warning: 'Close to limit',
  exceeded: 'Over budget',
}

export function BudgetsCard({ overview, style }: { overview: BudgetOverview; style?: CSSProperties }) {
  const overall = overview.totalBudget > 0 ? Math.round((overview.totalSpent / overview.totalBudget) * 100) : 0

  return (
    <section className="card budgets" style={style} aria-label="Budgets">
      <div className="card__header">
        <h2 className="card__title">Budgets</h2>
        <Link to="/categories" className="link">
          Manage →
        </Link>
      </div>

      {overview.categories.length === 0 ? (
        <EmptyState title="No budgets yet">
          <p className="muted">Set a monthly budget on a category to track it here.</p>
        </EmptyState>
      ) : (
        <>
          <p className="budgets__overall">
            <strong>{formatCurrency(overview.totalSpent)}</strong> of {formatCurrency(overview.totalBudget)} budgeted
            <span className="muted"> · {overall}%</span>
          </p>
          <ul className="budgets__list">
            {overview.categories.map((b) => (
              <li key={b.categoryId} className={`budget budget--${b.state}`}>
                <div className="budget__row">
                  <span className="budget__name">
                    <span className="legend__swatch" style={{ background: b.categoryColor }} />
                    {b.categoryName}
                  </span>
                  <span className="budget__state">{STATE_LABEL[b.state]}</span>
                </div>
                <div
                  className="budget__bar"
                  role="progressbar"
                  aria-label={`${b.categoryName} budget used`}
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-valuenow={Math.min(100, Math.round(b.percentUsed))}
                >
                  <span className="budget__fill" style={{ '--pct': `${Math.min(100, b.percentUsed)}%` } as CSSProperties} />
                </div>
                <div className="budget__row budget__numbers">
                  <span>
                    {formatCurrency(b.spent)} of {formatCurrency(b.budget)}
                  </span>
                  <span>
                    {b.remaining >= 0 ? `${formatCurrency(b.remaining)} left` : `${formatCurrency(-b.remaining)} over`}
                  </span>
                </div>
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  )
}
