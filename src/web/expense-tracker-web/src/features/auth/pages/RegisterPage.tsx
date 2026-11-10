import { Link, Navigate } from 'react-router-dom'
import { useSession } from '@/shared/auth/useSession'
import { ENV_LABEL } from '@/shared/config/env'
import { Field } from '@/shared/ui/Field'
import { AlertTriangleIcon, WalletIcon } from '@/shared/ui/Icons'
import { PasswordInput } from '@/shared/ui/PasswordInput'
import { useRegisterViewModel } from '../viewModels/useRegisterViewModel'

/** View: create your own account. Reuses the sign-in screen's layout. */
export function RegisterPage() {
  const vm = useRegisterViewModel()
  const { status } = useSession()

  if (status === 'authenticated') return <Navigate to="/" replace />

  return (
    <div className="auth-screen">
      <section className="auth-card card" aria-labelledby="register-title">
        <div className="auth-card__brand">
          <span className="brand__mark">
            <WalletIcon size={20} />
          </span>
          <span className="brand__name">Expense Tracker</span>
          {ENV_LABEL && <span className="env-badge">{ENV_LABEL}</span>}
        </div>

        <h1 id="register-title" className="auth-card__title">Create your account</h1>
        <p className="auth-card__subtitle">Then create a family and add the people you share expenses with.</p>

        {vm.formError && (
          <p className="auth-alert auth-alert--error" role="alert">
            <AlertTriangleIcon size={16} /> {vm.formError}
          </p>
        )}

        <form className="form" onSubmit={vm.submit} noValidate>
          <Field label="Name" error={vm.errors.displayName} htmlFor="register-name">
            <input id="register-name" className="input" value={vm.draft.displayName} onChange={(e) => vm.set('displayName', e.target.value)} maxLength={100} autoFocus />
          </Field>
          <Field label="Email" error={vm.errors.email} htmlFor="register-email">
            <input id="register-email" className="input" type="email" autoComplete="username" value={vm.draft.email} onChange={(e) => vm.set('email', e.target.value)} maxLength={256} />
          </Field>
          <Field label="Password" error={vm.errors.password} htmlFor="register-password">
            <PasswordInput id="register-password" autoComplete="new-password" value={vm.draft.password} onChange={(e) => vm.set('password', e.target.value)} />
          </Field>
          <p className="muted form-hint">{vm.passwordHint}</p>
          <button type="submit" className="button button--primary auth-card__submit" disabled={vm.submitting}>
            {vm.submitting ? 'Creating account…' : 'Create account'}
          </button>
        </form>

        <p className="muted auth-card__footnote">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </section>
    </div>
  )
}
