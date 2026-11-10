import { useState, type FormEvent } from 'react'
import { PASSWORD_HINT, passwordProblem } from '@/shared/auth/passwordPolicy'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'
import { useToast } from '@/shared/toast/toast'
import { Field } from '@/shared/ui/Field'
import { PasswordInput } from '@/shared/ui/PasswordInput'
import { FAMILY_ROLES, FAMILY_ROLE_LABEL, type AddMemberInput, type FamilyRole } from '../model/types'

interface AddMemberFormProps {
  onSubmit: (input: AddMemberInput) => Promise<void>
  onCancel: () => void
}

const EMAIL = /^[^\s@]+@[^\s@]+$/

/** Creates an account for someone and adds them to the family (Owner only). */
export function AddMemberForm({ onSubmit, onCancel }: AddMemberFormProps) {
  const [draft, setDraft] = useState<AddMemberInput>({ email: '', displayName: '', password: '', role: 'member' })
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  const set = <K extends keyof AddMemberInput>(key: K, value: AddMemberInput[K]) => {
    setDraft((d) => ({ ...d, [key]: value }))
    setErrors((e) => ({ ...e, [key]: '' }))
  }

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    const next: Record<string, string> = {}
    if (!draft.displayName.trim()) next.displayName = 'Enter a name.'
    if (!EMAIL.test(draft.email.trim())) next.email = 'Enter a valid email address.'
    const problem = passwordProblem(draft.password)
    if (problem) next.password = problem
    setErrors(next)
    if (Object.keys(next).length > 0) return

    setSaving(true)
    try {
      await onSubmit({ ...draft, email: draft.email.trim(), displayName: draft.displayName.trim() })
    } catch (err) {
      const fieldErrors = fieldErrorsOf(err)
      if (fieldErrors) setErrors(fieldErrors)
      else toast.error("Couldn't add the member", { description: errorMessage(err) })
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="form" onSubmit={submit} noValidate>
      <div className="form-row">
        <Field label="Name" error={errors.displayName} htmlFor="member-name">
          <input id="member-name" className="input" value={draft.displayName} onChange={(e) => set('displayName', e.target.value)} maxLength={100} data-autofocus />
        </Field>
        <Field label="Email" error={errors.email} htmlFor="member-email">
          <input id="member-email" className="input" type="email" autoComplete="off" value={draft.email} onChange={(e) => set('email', e.target.value)} maxLength={256} />
        </Field>
      </div>

      <Field label="Initial password" error={errors.password} htmlFor="member-password">
        <PasswordInput id="member-password" autoComplete="new-password" value={draft.password} onChange={(e) => set('password', e.target.value)} />
      </Field>
      <p className="muted form-hint">{PASSWORD_HINT} Share it securely; they can change it from their account page.</p>

      <Field label="Role in the family" error={errors.role} htmlFor="member-role">
        <select id="member-role" className="input" value={draft.role} onChange={(e) => set('role', e.target.value as FamilyRole)}>
          {FAMILY_ROLES.map((role) => (
            <option key={role} value={role}>
              {FAMILY_ROLE_LABEL[role]}
            </option>
          ))}
        </select>
      </Field>

      <div className="form-actions">
        <button type="button" className="button button--ghost" onClick={onCancel}>
          Cancel
        </button>
        <button type="submit" className="button button--primary" disabled={saving}>
          {saving ? 'Adding…' : 'Add member'}
        </button>
      </div>
    </form>
  )
}
