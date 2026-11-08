import { formatCurrency, formatDate } from '@/shared/lib/format'
import { staggerIndex } from '@/shared/lib/motion'
import type { Expense } from '../model/types'
import { CategoryBadge } from '@/shared/ui/CategoryBadge'
import { PencilIcon, TrashIcon } from '@/shared/ui/Icons'

interface ExpenseListProps {
  expenses: Expense[]
  onEdit?: (expense: Expense) => void
  onDelete?: (expense: Expense) => void
}

export function ExpenseList({ expenses, onEdit, onDelete }: ExpenseListProps) {
  const hasActions = Boolean(onEdit || onDelete)

  return (
    <ul className={`expense-list stagger${hasActions ? ' expense-list--actions' : ''}`}>
      {expenses.map((expense, i) => (
        <li key={expense.id} className="expense-row" style={staggerIndex(i)}>
          <time className="expense-row__date" dateTime={expense.date}>
            {formatDate(expense.date)}
          </time>
          <div className="expense-row__main">
            <span className="expense-row__description">{expense.description}</span>
            {expense.notes && <span className="expense-row__notes">{expense.notes}</span>}
          </div>
          <div className="expense-row__category">
            <CategoryBadge name={expense.categoryName} color={expense.categoryColor} />
          </div>
          <span className="expense-row__amount">{formatCurrency(expense.amount)}</span>
          {hasActions && (
            <div className="expense-row__actions">
              {onEdit && (
                <button
                  type="button"
                  className="icon-button"
                  onClick={() => onEdit(expense)}
                  aria-label={`Edit ${expense.description}`}
                  title="Edit"
                >
                  <PencilIcon size={16} />
                </button>
              )}
              {onDelete && (
                <button
                  type="button"
                  className="icon-button icon-button--danger"
                  onClick={() => onDelete(expense)}
                  aria-label={`Delete ${expense.description}`}
                  title="Delete"
                >
                  <TrashIcon size={16} />
                </button>
              )}
            </div>
          )}
        </li>
      ))}
    </ul>
  )
}
