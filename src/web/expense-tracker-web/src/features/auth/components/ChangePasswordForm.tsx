import { useState, type FormEvent } from 'react'
import { PASSWORD_HINT, passwordProblem } from '@/shared/auth/passwordPolicy'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'
import { useToast } from '@/shared/toast/toast'
import { Field } from '@/shared/ui/Field'
import { PasswordInput } from '@/shared/ui/PasswordInput'
import type { ChangePasswordInput } from '../model/types'

type Draft = ChangePasswordInput & { confirm: string }
type Errors = Partial<Record<keyof Draft, string>>

const EMPTY: Draft = { currentPassword: '', newPassword: '', confirm: '' }

export function ChangePasswordForm({ onSubmit }: { onSubmit: (input: ChangePasswordInput) => Promise<void> }) {
  const [draft, setDraft] = useState<Draft>(EMPTY)
  const [errors, setErrors] = useState<Errors>({})
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  const set = (key: keyof Draft, value: string) => {
    setDraft((d) => ({ ...d, [key]: value }))
    setErrors((e) => ({ ...e, [key]: undefined }))
  }

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    const next: Errors = {}
    if (!draft.currentPassword) next.currentPassword = 'Enter your current password.'
    const problem = passwordProblem(draft.newPassword)
    if (problem) next.newPassword = problem
    else if (draft.newPassword === draft.currentPassword) next.newPassword = 'Choose a password different from the current one.'
    if (draft.confirm !== draft.newPassword) next.confirm = "The passwords don't match."
    setErrors(next)
    if (Object.keys(next).length > 0) return

    setSaving(true)
    try {
      await onSubmit({ currentPassword: draft.currentPassword, newPassword: draft.newPassword })
      setDraft(EMPTY)
    } catch (err) {
      const fieldErrors = fieldErrorsOf(err)
      if (fieldErrors) setErrors(fieldErrors)
      else toast.error("Couldn't change your password", { description: errorMessage(err) })
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="form" onSubmit={submit} noValidate>
      <Field label="Current password" error={errors.currentPassword} htmlFor="current-password">
        <PasswordInput
          id="current-password"
          autoComplete="current-password"
          value={draft.currentPassword}
          onChange={(e) => set('currentPassword', e.target.value)}
        />
      </Field>
      <div className="form-row">
        <Field label="New password" error={errors.newPassword} htmlFor="new-password">
          <PasswordInput id="new-password" autoComplete="new-password" value={draft.newPassword} onChange={(e) => set('newPassword', e.target.value)} />
        </Field>
        <Field label="Confirm new password" error={errors.confirm} htmlFor="confirm-password">
          <PasswordInput id="confirm-password" autoComplete="new-password" value={draft.confirm} onChange={(e) => set('confirm', e.target.value)} />
        </Field>
      </div>
      <p className="muted form-hint">{PASSWORD_HINT} Changing it signs out your other sessions.</p>
      <div className="form-actions">
        <button type="submit" className="button button--primary" disabled={saving}>
          {saving ? 'Saving…' : 'Change password'}
        </button>
      </div>
    </form>
  )
}
