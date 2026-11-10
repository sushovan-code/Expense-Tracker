import { Link } from 'react-router-dom'
import { ENV_LABEL } from '@/shared/config/env'
import { Field } from '@/shared/ui/Field'
import { AlertTriangleIcon, WalletIcon } from '@/shared/ui/Icons'
import { PasswordInput } from '@/shared/ui/PasswordInput'
import { LoadingState } from '@/shared/ui/States'
import { useAcceptInviteViewModel } from '../viewModels/useAcceptInviteViewModel'

/** View: opened from an invite link. Reuses the sign-in screen's layout. */
export function AcceptInvitePage() {
  const vm = useAcceptInviteViewModel()

  if (vm.isRestoring) return <LoadingState label="Checking your session…" />

  return (
    <div className="auth-screen">
      <section className="auth-card card" aria-labelledby="invite-title">
        <div className="auth-card__brand">
          <span className="brand__mark">
            <WalletIcon size={20} />
          </span>
          <span className="brand__name">Expense Tracker</span>
          {ENV_LABEL && <span className="env-badge">{ENV_LABEL}</span>}
        </div>

        <h1 id="invite-title" className="auth-card__title">Join a family</h1>

        {!vm.hasToken ? (
          <p className="auth-alert auth-alert--error" role="alert">
            <AlertTriangleIcon size={16} /> This invite link is incomplete. Ask for a new one.
          </p>
        ) : (
          <>
            <p className="auth-card__subtitle">
              {vm.isSignedIn ? `You are signed in as ${vm.signedInAs}.` : 'You were invited. Create your account to join.'}
            </p>

            {vm.formError && (
              <p className="auth-alert auth-alert--error" role="alert">
                <AlertTriangleIcon size={16} /> {vm.formError}
              </p>
            )}

            {vm.isSignedIn ? (
              <button type="button" className="button button--primary auth-card__submit" onClick={() => void vm.join()} disabled={vm.busy}>
                {vm.busy ? 'Joining…' : 'Join with this account'}
              </button>
            ) : (
              <form className="form" onSubmit={vm.submit} noValidate>
                <Field label="Name" error={vm.errors.displayName} htmlFor="invite-name">
                  <input id="invite-name" className="input" value={vm.draft.displayName} onChange={(e) => vm.set('displayName', e.target.value)} maxLength={100} autoFocus />
                </Field>
                <Field label="Email" error={vm.errors.email} htmlFor="invite-email">
                  <input id="invite-email" className="input" type="email" autoComplete="username" value={vm.draft.email} onChange={(e) => vm.set('email', e.target.value)} maxLength={256} />
                </Field>
                <Field label="Password" error={vm.errors.password} htmlFor="invite-password">
                  <PasswordInput id="invite-password" autoComplete="new-password" value={vm.draft.password} onChange={(e) => vm.set('password', e.target.value)} />
                </Field>
                <p className="muted form-hint">{vm.passwordHint}</p>
                <button type="submit" className="button button--primary auth-card__submit" disabled={vm.busy}>
                  {vm.busy ? 'Creating account…' : 'Create account and join'}
                </button>
              </form>
            )}
          </>
        )}

        {!vm.isSignedIn && (
          <p className="muted auth-card__footnote">
            Already have an account? <Link to="/login">Sign in</Link>, then open this link again.
          </p>
        )}
      </section>
    </div>
  )
}
