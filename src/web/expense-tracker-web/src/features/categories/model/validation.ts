import type { CategoryInput } from './types'

/** Editable category fields as typed by the user (budget is still text; empty = no budget). */
export interface CategoryDraft {
  name: string
  color: string
  monthlyBudget: string
}

export type CategoryErrors = Partial<Record<keyof CategoryInput, string>>

export function validateCategory(draft: CategoryDraft): CategoryErrors {
  const errors: CategoryErrors = {}
  if (!draft.name.trim()) errors.name = 'Name is required.'
  else if (draft.name.trim().length > 50) errors.name = 'Keep it under 50 characters.'
  if (!/^#[0-9a-f]{6}$/i.test(draft.color)) errors.color = 'Pick a valid color.'
  if (draft.monthlyBudget.trim() !== '') {
    const budget = Number(draft.monthlyBudget)
    if (Number.isNaN(budget) || budget < 0) errors.monthlyBudget = 'Budget must be zero or more (or empty for none).'
  }
  return errors
}

export const toCategoryInput = (draft: CategoryDraft): CategoryInput => ({
  name: draft.name.trim(),
  color: draft.color.toLowerCase(),
  monthlyBudget: draft.monthlyBudget.trim() === '' ? null : Math.round(Number(draft.monthlyBudget) * 100) / 100,
})

export const hasErrors = (errors: object) => Object.keys(errors).length > 0
