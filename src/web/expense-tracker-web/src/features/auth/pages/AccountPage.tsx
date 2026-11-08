import { useState } from 'react'
import { formatTimestamp } from '@/shared/lib/format'
import { useToast } from '@/shared/toast/toast'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { CheckIcon, CopyIcon, KeyIcon, PlusIcon, TrashIcon } from '@/shared/ui/Icons'
import { Modal } from '@/shared/ui/Modal'
import { EmptyState, ErrorState, LoadingState } from '@/shared/ui/States'
import { ApiKeyForm } from '../components/ApiKeyForm'
import { ChangePasswordForm } from '../components/ChangePasswordForm'
import { apiKeyStatus } from '../model/types'
import { useAccountViewModel } from '../viewModels/useAccountViewModel'

/** View: the signed-in user's profile, password and API keys. */
export function AccountPage() {
  const vm = useAccountViewModel()
  const { user, apiKeys } = vm
  if (!user) return null

  return (
    <>
      <div className="page-header">
        <div>
          <h1 className="page-title">Your account</h1>
          <p className="page-subtitle">{user.email}</p>
        </div>
      </div>

      <div className="account-grid">
        <section className="card">
          <h2 className="card__title">Profile &amp; access</h2>
          <dl className="details">
            <dt>Name</dt>
            <dd>{user.displayName}</dd>
            <dt>Roles</dt>
            <dd className="chips">
              {user.roles.map((role) => (
                <span key={role} className={`role-badge role-badge--${role.toLowerCase()}`}>
                  {role}
                </span>
              ))}
            </dd>
            <dt>Permissions</dt>
            <dd className="chips">
              {user.permissions.map((permission) => (
                <span key={permission} className="chip">
                  {permission}
                </span>
              ))}
            </dd>
          </dl>
        </section>

        <section className="card">
          <h2 className="card__title">Change password</h2>
          <ChangePasswordForm onSubmit={vm.changePassword} />
        </section>
      </div>

      {apiKeys.enabled && (
        <section className="card card--flush account-keys">
          <div className="card__header card__header--padded">
            <div>
              <h2 className="card__title">API keys</h2>
              <p className="muted">
                For scripts and integrations. Send the key in the <code>x-api-key</code> header; it acts as you, limited to the permissions you pick.
              </p>
            </div>
            <button type="button" className="button button--primary" onClick={vm.createDialog.open}>
              <PlusIcon size={16} /> New key
            </button>
          </div>

          {apiKeys.error && !apiKeys.data ? (
            <ErrorState error={apiKeys.error} onRetry={apiKeys.retry} />
          ) : apiKeys.isLoading || !apiKeys.data ? (
            <LoadingState />
          ) : apiKeys.data.length === 0 ? (
            <EmptyState title="No API keys yet" />
          ) : (
            <div className="grid-wrap">
              <table className="grid grid--keys">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Key</th>
                    <th>Access</th>
                    <th>Expires</th>
                    <th>Last used</th>
                    <th>
                      <span className="sr-only">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {apiKeys.data.map((key) => {
                    const status = apiKeyStatus(key)
                    return (
                      <tr key={key.id} className={`grid-row${status === 'active' ? '' : ' grid-row--muted'}`}>
                        <td>
                          <span className="cell-strong">{key.name}</span>
                          {status !== 'active' && <span className={`status-pill status-pill--${status}`}>{status}</span>}
                        </td>
                        <td>
                          <code>{key.prefix}…</code>
                        </td>
                        <td className="cell-muted" title={key.scopes.join(', ')}>
                          {key.scopes.length === 0 ? 'All of yours' : `${key.scopes.length} permission${key.scopes.length === 1 ? '' : 's'}`}
                        </td>
                        <td className="cell-muted">{formatTimestamp(key.expiresAt)}</td>
                        <td className="cell-muted">{key.lastUsedAt ? formatTimestamp(key.lastUsedAt) : 'Never'}</td>
                        <td className="col-actions">
                          {status === 'active' && (
                            <button type="button" className="icon-button icon-button--danger" onClick={() => vm.revokeDialog.request(key)} aria-label={`Revoke ${key.name}`} title="Revoke">
                              <TrashIcon size={16} />
                            </button>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}

      <Modal open={vm.createDialog.isOpen} title="New API key" description="Keys always expire. Limit each key to what it needs." onClose={vm.createDialog.close}>
        <ApiKeyForm grantable={apiKeys.grantable} onSubmit={vm.createDialog.submit} onCancel={vm.createDialog.close} />
      </Modal>

      <Modal open={vm.revealDialog.created !== null} title="Copy your API key" description="This is the only time it is shown. Store it somewhere safe." onClose={vm.revealDialog.close}>
        {vm.revealDialog.created && <RevealedKey value={vm.revealDialog.created.key} onDone={vm.revealDialog.close} />}
      </Modal>

      <ConfirmDialog
        open={vm.revokeDialog.target !== null}
        title="Revoke API key?"
        message={
          <>
            Anything using <strong>{vm.revokeDialog.target?.name}</strong> ({vm.revokeDialog.target?.prefix}…) will stop working immediately.
          </>
        }
        confirmLabel="Revoke"
        busyLabel="Revoking…"
        onConfirm={vm.revokeDialog.confirm}
        onClose={vm.revokeDialog.cancel}
      />
    </>
  )
}

function RevealedKey({ value, onDone }: { value: string; onDone: () => void }) {
  const [copied, setCopied] = useState(false)
  const toast = useToast()

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(value)
      setCopied(true)
    } catch {
      toast.warning("Couldn't copy automatically", { description: 'Select the key and copy it manually.' })
    }
  }

  return (
    <div className="form">
      <div className="secret">
        <KeyIcon size={16} />
        <code className="secret__value">{value}</code>
        <button type="button" className="icon-button" onClick={copy} aria-label="Copy key" title="Copy" data-autofocus>
          {copied ? <CheckIcon size={16} /> : <CopyIcon size={16} />}
        </button>
      </div>
      <div className="form-actions">
        <button type="button" className="button button--primary" onClick={onDone}>
          I've saved it
        </button>
      </div>
    </div>
  )
}
