import { useState, type FormEvent } from 'react'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'
import { today } from '@/shared/lib/format'
import { hasErrors, toExpenseInput, validateExpense, type ExpenseErrors } from '../model/validation'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import type { CategoryOption, Expense, ExpenseInput } from '../model/types'
import { Field } from '@/shared/ui/Field'

interface ExpenseFormProps {
  expense?: Expense
  categories: CategoryOption[]
  onSubmit: (input: ExpenseInput) => Promise<void>
  onCancel: () => void
}

export function ExpenseForm({ expense, categories, onSubmit, onCancel }: ExpenseFormProps) {
  const [description, setDescription] = useState(expense?.description ?? '')
  const [amount, setAmount] = useState(expense ? String(expense.amount) : '')
  const [date, setDate] = useState(expense?.date ?? today())
  const [categoryId, setCategoryId] = useState(expense?.categoryId ?? categories[0]?.id ?? '')
  const [notes, setNotes] = useState(expense?.notes ?? '')
  const [errors, setErrors] = useState<ExpenseErrors>({})
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    const draft = { description, amount, date, categoryId, notes }
    const clientErrors = validateExpense(draft)
    setErrors(clientErrors)
    if (hasErrors(clientErrors)) {
      logger.event('expense.validation_failed', 'Expense form has invalid fields', {
        'validation.fields': Object.keys(clientErrors).join(','),
        'ui.source': 'dialog',
      }, 'warn')
      toast.warning('Please fix the highlighted fields', { description: Object.values(clientErrors)[0] })
      return
    }

    setSaving(true)
    try {
      await onSubmit(toExpenseInput(draft))
    } catch (err) {
      const fieldErrors = fieldErrorsOf(err)
      if (fieldErrors) {
        setErrors(fieldErrors)
        toast.warning('Please fix the highlighted fields', { description: Object.values(fieldErrors)[0] })
      } else {
        toast.error("Couldn't save", { description: errorMessage(err) })
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="form" onSubmit={handleSubmit} noValidate>
      <Field label="Description" error={errors.description} htmlFor="expense-description">
        <input
          id="expense-description"
          className="input"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          maxLength={200}
          placeholder="e.g. Groceries"
          data-autofocus
        />
      </Field>

      <div className="form-row">
        <Field label="Amount" error={errors.amount} htmlFor="expense-amount">
          <input
            id="expense-amount"
            className="input"
            type="number"
            inputMode="decimal"
            min="0.01"
            step="0.01"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            placeholder="0.00"
          />
        </Field>
        <Field label="Date" error={errors.date} htmlFor="expense-date">
          <input
            id="expense-date"
            className="input"
            type="date"
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
        </Field>
      </div>

      <Field label="Category" error={errors.categoryId} htmlFor="expense-category">
        <select
          id="expense-category"
          className="input"
          value={categoryId}
          onChange={(e) => setCategoryId(e.target.value)}
        >
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </Field>

      <Field label="Notes (optional)" error={errors.notes} htmlFor="expense-notes">
        <textarea
          id="expense-notes"
          className="input"
          rows={3}
          maxLength={1000}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
        />
      </Field>


      <div className="form-actions">
        <button type="button" className="button button--ghost" onClick={onCancel}>
          Cancel
        </button>
        <button type="submit" className="button button--primary" disabled={saving}>
          {saving ? 'Saving…' : expense ? 'Save changes' : 'Add expense'}
        </button>
      </div>
    </form>
  )
}
