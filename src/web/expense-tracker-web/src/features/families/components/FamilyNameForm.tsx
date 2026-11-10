import { useState, type FormEvent } from 'react'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'
import { useToast } from '@/shared/toast/toast'
import { Field } from '@/shared/ui/Field'

interface FamilyNameFormProps {
  initialName?: string
  submitLabel: string
  onSubmit: (name: string) => Promise<void>
}

const NAME_MAX_LENGTH = 100

/** One name field with a button: used to create a family and to rename it. */
export function FamilyNameForm({ initialName = '', submitLabel, onSubmit }: FamilyNameFormProps) {
  const [name, setName] = useState(initialName)
  const [error, setError] = useState<string>()
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (!name.trim()) {
      setError('Enter a name.')
      return
    }
    setError(undefined)
    setSaving(true)
    try {
      await onSubmit(name.trim())
    } catch (err) {
      const fieldError = fieldErrorsOf(err)?.name
      if (fieldError) setError(fieldError)
      else toast.error("Couldn't save the family", { description: errorMessage(err) })
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="form" onSubmit={submit} noValidate>
      <Field label="Family name" error={error} htmlFor="family-name">
        <input id="family-name" className="input" value={name} maxLength={NAME_MAX_LENGTH} placeholder="e.g. The Sharma family" onChange={(e) => setName(e.target.value)} />
      </Field>
      <div className="form-actions">
        <button type="submit" className="button button--primary" disabled={saving}>
          {saving ? 'Saving…' : submitLabel}
        </button>
      </div>
    </form>
  )
}
