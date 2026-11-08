import type { ExpenseInput } from './types'

/** Editable expense fields as typed by the user (amount is still text). */
export interface ExpenseDraft {
  description: string
  amount: string
  date: string
  categoryId: number
  notes: string
}

export type ExpenseErrors = Partial<Record<keyof ExpenseInput, string>>

/** Mirrors the API/domain rules so users get instant feedback; the server remains the authority. */
export function validateExpense(draft: ExpenseDraft): ExpenseErrors {
  const errors: ExpenseErrors = {}
  if (!draft.description.trim()) errors.description = 'Description is required.'
  else if (draft.description.trim().length > 200) errors.description = 'Keep it under 200 characters.'
  const amount = Number(draft.amount)
  if (!draft.amount || Number.isNaN(amount) || amount <= 0) errors.amount = 'Enter an amount greater than zero.'
  else if (Math.round(amount * 100) / 100 !== amount) errors.amount = 'Use at most two decimal places.'
  if (!draft.date) errors.date = 'Date is required.'
  if (!draft.categoryId) errors.categoryId = 'Pick a category.'
  if (draft.notes.length > 1000) errors.notes = 'Keep notes under 1000 characters.'
  return errors
}

export const toExpenseInput = (draft: ExpenseDraft): ExpenseInput => ({
  description: draft.description.trim(),
  amount: Number(draft.amount),
  date: draft.date,
  categoryId: draft.categoryId,
  notes: draft.notes.trim() || null,
})

export const hasErrors = (errors: object) => Object.keys(errors).length > 0
