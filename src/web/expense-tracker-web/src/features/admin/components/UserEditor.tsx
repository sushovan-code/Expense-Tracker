import { useState, type FormEvent } from 'react'
import { PASSWORD_HINT, passwordProblem } from '@/shared/auth/passwordPolicy'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'
import { formatTimestamp } from '@/shared/lib/format'
import { useToast } from '@/shared/toast/toast'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { Field } from '@/shared/ui/Field'
import { InfoIcon, LogOutIcon, PlusIcon, TrashIcon } from '@/shared/ui/Icons'
import { PasswordInput } from '@/shared/ui/PasswordInput'
import { ErrorState, LoadingState } from '@/shared/ui/States'
import { PERMISSION_CLAIM, type UserDetail } from '../model/types'
import { ROLE_ORDER } from '../viewModels/useAdminAccess'
import type { UserEditorViewModel } from '../viewModels/useUserEditorViewModel'
import { UserStatus } from './UserStatus'

/** Body of the "Manage user" dialog. */
export function UserEditor({ vm }: { vm: UserEditorViewModel }) {
  if (vm.error && !vm.user) return <ErrorState error={vm.error} onRetry={vm.retry} />
  if (vm.isLoading || !vm.user) return <LoadingState />
  const user = vm.user

  return (
    <div className="user-editor">
      <div className="user-editor__summary">
        <UserStatus user={user} />
        <span className="muted">
          Last sign-in {user.lastLoginAt ? formatTimestamp(user.lastLoginAt) : 'never'} · created {formatTimestamp(user.createdAt)}
        </span>
      </div>

      {vm.readOnlyReason && (
        <p className="auth-alert auth-alert--info">
          <InfoIcon size={16} /> {vm.readOnlyReason}
        </p>
      )}

      <section className="editor-section">
        <h3 className="editor-section__title">Profile</h3>
        {/* Keyed by user + version so the form resets when the data changes underneath it. */}
        <ProfileForm key={`${user.id}:${user.email}:${user.displayName}:${user.isActive}`} user={user} disabled={!vm.canEdit} onSave={vm.saveProfile} />
      </section>

      <section className="editor-section">
        <h3 className="editor-section__title">Roles</h3>
        <RolesForm key={user.roles.join(',')} current={user.roles} assignable={vm.assignableRoles} disabled={!vm.canEdit} onSave={vm.saveRoles} />
      </section>

      <section className="editor-section">
        <h3 className="editor-section__title">Claims</h3>
        <p className="muted form-hint">
          A <code>permission</code> claim grants that permission directly, on top of the user's roles. Other claim types are stored as user attributes.
        </p>
        <ClaimsEditor vm={vm} user={user} />
      </section>

      <section className="editor-section">
        <h3 className="editor-section__title">Effective permissions</h3>
        <div className="chips">
          {user.effectivePermissions.length === 0 ? (
            <span className="muted">None</span>
          ) : (
            user.effectivePermissions.map((permission) => (
              <span key={permission} className="chip">
                {permission}
              </span>
            ))
          )}
        </div>
      </section>

      {vm.canEdit && <SecuritySection vm={vm} user={user} />}

      <ConfirmDialog
        open={vm.deleteDialog.isOpen}
        title="Delete user?"
        message={
          <>
            <strong>{user.displayName}</strong> ({user.email}) will be removed, together with their sessions and API keys. This can't be undone.
          </>
        }
        onConfirm={vm.deleteDialog.confirm}
        onClose={vm.deleteDialog.cancel}
      />
    </div>
  )
}

function ProfileForm({ user, disabled, onSave }: { user: UserDetail; disabled: boolean; onSave: UserEditorViewModel['saveProfile'] }) {
  const [draft, setDraft] = useState({ email: user.email, displayName: user.displayName, isActive: user.isActive })
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const dirty = draft.email !== user.email || draft.displayName !== user.displayName || draft.isActive !== user.isActive

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (!draft.displayName.trim()) return setErrors({ displayName: 'Enter a name.' })
    setSaving(true)
    try {
      await onSave({ ...draft, email: draft.email.trim(), displayName: draft.displayName.trim() })
    } catch (err) {
      const fieldErrors = fieldErrorsOf(err)
      if (fieldErrors) setErrors(fieldErrors)
      else toast.error("Couldn't update the user", { description: errorMessage(err) })
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="form" onSubmit={submit} noValidate>
      <div className="form-row">
        <Field label="Name" error={errors.displayName} htmlFor="edit-user-name">
          <input id="edit-user-name" className="input" value={draft.displayName} disabled={disabled} maxLength={100}
            onChange={(e) => setDraft((d) => ({ ...d, displayName: e.target.value }))} />
        </Field>
        <Field label="Email" error={errors.email} htmlFor="edit-user-email">
          <input id="edit-user-email" className="input" type="email" value={draft.email} disabled={disabled} maxLength={256}
            onChange={(e) => setDraft((d) => ({ ...d, email: e.target.value }))} />
        </Field>
      </div>
      <label className="choice">
        <input type="checkbox" checked={draft.isActive} disabled={disabled} onChange={(e) => setDraft((d) => ({ ...d, isActive: e.target.checked }))} />
        <span>
          Active <span className="muted">— disabling blocks sign-in and ends the user's sessions and API keys immediately</span>
        </span>
      </label>
      {!disabled && (
        <div className="form-actions">
          <button type="submit" className="button button--primary button--sm" disabled={!dirty || saving}>
            {saving ? 'Saving…' : 'Save profile'}
          </button>
        </div>
      )}
    </form>
  )
}

function RolesForm({ current, assignable, disabled, onSave }: {
  current: string[]
  assignable: string[]
  disabled: boolean
  onSave: (roles: string[]) => Promise<boolean>
}) {
  const [selected, setSelected] = useState(current)
  const [saving, setSaving] = useState(false)
  const dirty = selected.length !== current.length || selected.some((r) => !current.includes(r))
  const toggle = (role: string) => setSelected((s) => (s.includes(role) ? s.filter((r) => r !== role) : [...s, role]))

  return (
    <div className="form">
      <div className="permission-picker">
        {ROLE_ORDER.map((role) => {
          const allowed = !disabled && assignable.includes(role)
          return (
            <label key={role} className="chip-toggle" title={allowed || disabled ? undefined : 'Only roles below your own can be assigned or removed'}>
              <input type="checkbox" checked={selected.includes(role)} disabled={!allowed} onChange={() => toggle(role)} />
              <span>{role}</span>
            </label>
          )
        })}
      </div>
      {!disabled && (
        <div className="form-actions">
          <button type="button" className="button button--primary button--sm" disabled={!dirty || saving || selected.length === 0}
            onClick={async () => {
              setSaving(true)
              await onSave(selected)
              setSaving(false)
            }}>
            {saving ? 'Saving…' : 'Save roles'}
          </button>
        </div>
      )}
    </div>
  )
}

function ClaimsEditor({ vm, user }: { vm: UserEditorViewModel; user: UserDetail }) {
  const [type, setType] = useState(PERMISSION_CLAIM)
  const [value, setValue] = useState('')
  const isPermission = type.trim().toLowerCase() === PERMISSION_CLAIM
  const options = vm.grantablePermissions.filter((p) => !user.claims.some((c) => c.type === PERMISSION_CLAIM && c.value === p.name))

  const add = async (e: FormEvent) => {
    e.preventDefault()
    if (!type.trim() || !value.trim()) return
    if (await vm.addClaim({ type: type.trim(), value: value.trim() })) setValue('')
  }

  return (
    <>
      {user.claims.length === 0 ? (
        <p className="muted">No claims.</p>
      ) : (
        <ul className="claim-list">
          {user.claims.map((claim) => (
            <li key={`${claim.type}=${claim.value}`} className="claim-list__item">
              <code>{claim.type}</code>
              <span className="claim-list__value">{claim.value}</span>
              {vm.canEdit && (
                <button type="button" className="icon-button icon-button--danger" onClick={() => void vm.removeClaim(claim)} aria-label={`Remove claim ${claim.type} ${claim.value}`} title="Remove">
                  <TrashIcon size={14} />
                </button>
              )}
            </li>
          ))}
        </ul>
      )}

      {vm.canEdit && (
        <form className="claim-form" onSubmit={add} noValidate>
          <input className="input input--sm" list="claim-types" value={type} onChange={(e) => { setType(e.target.value); setValue('') }}
            placeholder="Type" aria-label="Claim type" maxLength={256} />
          <datalist id="claim-types">
            <option value={PERMISSION_CLAIM} />
            <option value="department" />
            <option value="cost_center" />
          </datalist>
          {isPermission ? (
            <select className="input input--sm" value={value} onChange={(e) => setValue(e.target.value)} aria-label="Permission">
              <option value="">Choose a permission…</option>
              {options.map((p) => (
                <option key={p.name} value={p.name}>
                  {p.name} — {p.description}
                </option>
              ))}
            </select>
          ) : (
            <input className="input input--sm" value={value} onChange={(e) => setValue(e.target.value)} placeholder="Value" aria-label="Claim value" maxLength={1024} />
          )}
          <button type="submit" className="button button--ghost button--sm" disabled={!type.trim() || !value.trim()}>
            <PlusIcon size={14} /> Add
          </button>
        </form>
      )}
    </>
  )
}

function SecuritySection({ vm, user }: { vm: UserEditorViewModel; user: UserDetail }) {
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  const reset = async (e: FormEvent) => {
    e.preventDefault()
    const problem = passwordProblem(password)
    setError(problem)
    if (!problem && (await vm.resetPassword(password))) setPassword('')
  }

  return (
    <section className="editor-section">
      <h3 className="editor-section__title">Security</h3>
      {user.isLockedOut && user.lockoutEnd && (
        <p className="auth-alert auth-alert--error">
          Locked after too many failed sign-ins, until {formatTimestamp(user.lockoutEnd)}.
        </p>
      )}

      <form className="form" onSubmit={reset} noValidate>
        <Field label="Set a new password" error={error ?? undefined} htmlFor="reset-password">
          <div className="inline-field">
            <PasswordInput id="reset-password" autoComplete="new-password" value={password} onChange={(e) => { setPassword(e.target.value); setError(null) }} />
            <button type="submit" className="button button--ghost button--sm" disabled={!password}>
              Reset password
            </button>
          </div>
        </Field>
        <p className="muted form-hint">{PASSWORD_HINT} Also unlocks the account and signs the user out everywhere.</p>
      </form>

      <div className="security-actions">
        {user.isLockedOut && (
          <button type="button" className="button button--ghost button--sm" onClick={() => void vm.unlock()}>
            Unlock account
          </button>
        )}
        <button type="button" className="button button--ghost button--sm" onClick={() => void vm.revokeSessions()}>
          <LogOutIcon size={14} /> Sign out everywhere
        </button>
        <button type="button" className="button button--danger button--sm" onClick={vm.deleteDialog.open}>
          <TrashIcon size={14} /> Delete user
        </button>
      </div>
    </section>
  )
}
