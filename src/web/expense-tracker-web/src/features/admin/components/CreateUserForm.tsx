import { useState, type FormEvent } from 'react'
import { PASSWORD_HINT, passwordProblem } from '@/shared/auth/passwordPolicy'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'
import { useToast } from '@/shared/toast/toast'
import { Field } from '@/shared/ui/Field'
import { PasswordInput } from '@/shared/ui/PasswordInput'
import type { CreateUserInput } from '../model/types'
import { ROLE_ORDER } from '../viewModels/useAdminAccess'

interface CreateUserFormProps {
  assignableRoles: string[]
  onSubmit: (input: CreateUserInput) => Promise<void>
  onCancel: () => void
}

const EMAIL = /^[^\s@]+@[^\s@]+$/

export function CreateUserForm({ assignableRoles, onSubmit, onCancel }: CreateUserFormProps) {
  const lowest = assignableRoles[assignableRoles.length - 1]
  const [draft, setDraft] = useState<CreateUserInput>({ email: '', displayName: '', password: '', roles: lowest ? [lowest] : [] })
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  const set = <K extends keyof CreateUserInput>(key: K, value: CreateUserInput[K]) => {
    setDraft((d) => ({ ...d, [key]: value }))
    setErrors((e) => ({ ...e, [key]: '' }))
  }

  const toggleRole = (role: string) =>
    set('roles', draft.roles.includes(role) ? draft.roles.filter((r) => r !== role) : [...draft.roles, role])

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    const next: Record<string, string> = {}
    if (!draft.displayName.trim()) next.displayName = 'Enter a name.'
    if (!EMAIL.test(draft.email.trim())) next.email = 'Enter a valid email address.'
    const problem = passwordProblem(draft.password)
    if (problem) next.password = problem
    if (draft.roles.length === 0) next.roles = 'Pick at least one role.'
    setErrors(next)
    if (Object.keys(next).length > 0) return

    setSaving(true)
    try {
      await onSubmit({ ...draft, email: draft.email.trim(), displayName: draft.displayName.trim() })
    } catch (err) {
      const fieldErrors = fieldErrorsOf(err)
      if (fieldErrors) setErrors(fieldErrors)
      else toast.error("Couldn't create the user", { description: errorMessage(err) })
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="form" onSubmit={submit} noValidate>
      <div className="form-row">
        <Field label="Name" error={errors.displayName} htmlFor="new-user-name">
          <input id="new-user-name" className="input" value={draft.displayName} onChange={(e) => set('displayName', e.target.value)} maxLength={100} data-autofocus />
        </Field>
        <Field label="Email" error={errors.email} htmlFor="new-user-email">
          <input id="new-user-email" className="input" type="email" autoComplete="off" value={draft.email} onChange={(e) => set('email', e.target.value)} maxLength={256} />
        </Field>
      </div>

      <Field label="Initial password" error={errors.password} htmlFor="new-user-password">
        <PasswordInput id="new-user-password" autoComplete="new-password" value={draft.password} onChange={(e) => set('password', e.target.value)} />
      </Field>
      <p className="muted form-hint">{PASSWORD_HINT} Share it securely; the user can change it from their account page.</p>

      <fieldset className="fieldset">
        <legend className="field__label">Roles</legend>
        <div className="permission-picker">
          {ROLE_ORDER.map((role) => (
            <label key={role} className="chip-toggle" title={assignableRoles.includes(role) ? undefined : 'Only roles below your own can be assigned'}>
              <input type="checkbox" checked={draft.roles.includes(role)} disabled={!assignableRoles.includes(role)} onChange={() => toggleRole(role)} />
              <span>{role}</span>
            </label>
          ))}
        </div>
        {errors.roles && <span className="field__error">{errors.roles}</span>}
      </fieldset>

      <div className="form-actions">
        <button type="button" className="button button--ghost" onClick={onCancel}>
          Cancel
        </button>
        <button type="submit" className="button button--primary" disabled={saving}>
          {saving ? 'Creating…' : 'Create user'}
        </button>
      </div>
    </form>
  )
}
