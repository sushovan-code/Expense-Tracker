import { useState, type FormEvent } from 'react'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'
import { hasErrors, toCategoryInput, validateCategory, type CategoryDraft, type CategoryErrors } from '../model/validation'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import type { Category, CategoryInput } from '../model/types'
import { Field } from '@/shared/ui/Field'

const PRESET_COLORS = [
  '#ef4444', '#f97316', '#eab308', '#22c55e', '#14b8a6', '#06b6d4',
  '#3b82f6', '#6366f1', '#8b5cf6', '#ec4899', '#64748b', '#a16207',
]

interface CategoryFormProps {
  category?: Category
  onSubmit: (input: CategoryInput) => Promise<void>
  onCancel: () => void
}

export function CategoryForm({ category, onSubmit, onCancel }: CategoryFormProps) {
  const [draft, setDraft] = useState<CategoryDraft>({
    name: category?.name ?? '',
    color: category?.color ?? PRESET_COLORS[6],
    monthlyBudget: category?.monthlyBudget != null ? String(category.monthlyBudget) : '',
  })
  const [errors, setErrors] = useState<CategoryErrors>({})
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  const set = <K extends keyof CategoryDraft>(key: K, value: CategoryDraft[K]) => setDraft((d) => ({ ...d, [key]: value }))

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    const clientErrors = validateCategory(draft)
    setErrors(clientErrors)
    if (hasErrors(clientErrors)) {
      logger.event('category.validation_failed', 'Category form has invalid fields', {
        'validation.fields': Object.keys(clientErrors).join(','),
        'ui.source': 'dialog',
      }, 'warn')
      toast.warning('Please fix the highlighted fields', { description: Object.values(clientErrors)[0] })
      return
    }

    setSaving(true)
    try {
      await onSubmit(toCategoryInput(draft))
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
      <Field label="Name" error={errors.name} htmlFor="category-name">
        <input
          id="category-name"
          className="input"
          value={draft.name}
          onChange={(e) => set('name', e.target.value)}
          maxLength={50}
          placeholder="e.g. Travel"
          data-autofocus
        />
      </Field>

      <Field label="Monthly budget (optional)" error={errors.monthlyBudget} htmlFor="category-budget">
        <input
          id="category-budget"
          className="input"
          type="number"
          inputMode="decimal"
          min="0"
          step="0.01"
          value={draft.monthlyBudget}
          onChange={(e) => set('monthlyBudget', e.target.value)}
          placeholder="No limit"
        />
      </Field>

      <Field label="Color" error={errors.color} htmlFor="category-color">
        <div className="color-picker">
          {PRESET_COLORS.map((preset) => (
            <button
              key={preset}
              type="button"
              className="color-swatch"
              style={{ background: preset }}
              aria-label={`Use color ${preset}`}
              aria-pressed={draft.color.toLowerCase() === preset}
              onClick={() => set('color', preset)}
            />
          ))}
          <input
            id="category-color"
            type="color"
            className="color-input"
            value={draft.color}
            onChange={(e) => set('color', e.target.value)}
            aria-label="Custom color"
          />
        </div>
      </Field>

      <div className="form-actions">
        <button type="button" className="button button--ghost" onClick={onCancel}>
          Cancel
        </button>
        <button type="submit" className="button button--primary" disabled={saving}>
          {saving ? 'Saving…' : category ? 'Save changes' : 'Add category'}
        </button>
      </div>
    </form>
  )
}
