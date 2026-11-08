import { useState, type FormEvent } from 'react'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'
import { useToast } from '@/shared/toast/toast'
import { Field } from '@/shared/ui/Field'
import type { ApiKeyInput } from '../model/types'

const LIFETIMES = [30, 90, 180, 365]

interface ApiKeyFormProps {
  /** Permissions the key may be limited to: the owner's own. */
  grantable: string[]
  onSubmit: (input: ApiKeyInput) => Promise<void>
  onCancel: () => void
}

export function ApiKeyForm({ grantable, onSubmit, onCancel }: ApiKeyFormProps) {
  const [name, setName] = useState('')
  const [expiresInDays, setExpiresInDays] = useState(90)
  const [limited, setLimited] = useState(true)
  const [scopes, setScopes] = useState<string[]>(grantable.filter((p) => p.endsWith('.view')))
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  const toggle = (permission: string) =>
    setScopes((current) => (current.includes(permission) ? current.filter((p) => p !== permission) : [...current, permission]))

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    const next: Record<string, string> = {}
    if (!name.trim()) next.name = 'Give the key a name, e.g. the script or service that will use it.'
    if (limited && scopes.length === 0) next.scopes = 'Pick at least one permission, or allow all of yours.'
    setErrors(next)
    if (Object.keys(next).length > 0) return

    setSaving(true)
    try {
      await onSubmit({ name: name.trim(), expiresInDays, scopes: limited ? scopes : [] })
    } catch (err) {
      const fieldErrors = fieldErrorsOf(err)
      if (fieldErrors) setErrors(fieldErrors)
      else toast.error("Couldn't create the key", { description: errorMessage(err) })
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="form" onSubmit={submit} noValidate>
      <div className="form-row">
        <Field label="Name" error={errors.name} htmlFor="api-key-name">
          <input id="api-key-name" className="input" value={name} onChange={(e) => setName(e.target.value)} maxLength={100} placeholder="e.g. Monthly report script" data-autofocus />
        </Field>
        <Field label="Expires after" error={errors.expiresInDays} htmlFor="api-key-expiry">
          <select id="api-key-expiry" className="input" value={expiresInDays} onChange={(e) => setExpiresInDays(Number(e.target.value))}>
            {LIFETIMES.map((days) => (
              <option key={days} value={days}>
                {days} days
              </option>
            ))}
          </select>
        </Field>
      </div>

      <fieldset className="fieldset">
        <legend className="field__label">Access</legend>
        <label className="choice">
          <input type="radio" name="api-key-scope" checked={limited} onChange={() => setLimited(true)} />
          <span>
            Only selected permissions <span className="muted">(recommended)</span>
          </span>
        </label>
        <label className="choice">
          <input type="radio" name="api-key-scope" checked={!limited} onChange={() => setLimited(false)} />
          <span>Everything I can do</span>
        </label>
        {limited && (
          <div className="permission-picker">
            {grantable.map((permission) => (
              <label key={permission} className="chip-toggle">
                <input type="checkbox" checked={scopes.includes(permission)} onChange={() => toggle(permission)} />
                <span>{permission}</span>
              </label>
            ))}
          </div>
        )}
        {errors.scopes && <span className="field__error">{errors.scopes}</span>}
      </fieldset>

      <div className="form-actions">
        <button type="button" className="button button--ghost" onClick={onCancel}>
          Cancel
        </button>
        <button type="submit" className="button button--primary" disabled={saving}>
          {saving ? 'Creating…' : 'Create key'}
        </button>
      </div>
    </form>
  )
}
