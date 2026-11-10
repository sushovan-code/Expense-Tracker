import { Link, Navigate } from 'react-router-dom'
import { ENV_LABEL } from '@/shared/config/env'
import { Field } from '@/shared/ui/Field'
import { AlertTriangleIcon, InfoIcon, LockIcon, WalletIcon } from '@/shared/ui/Icons'
import { PasswordInput } from '@/shared/ui/PasswordInput'
import { LoadingState } from '@/shared/ui/States'
import { SignInQrButton } from '../components/SignInQrButton'
import { useLoginViewModel } from '../viewModels/useLoginViewModel'

/** Accounts seeded by the API in Development (configs/appsettings.Development.json). */
const DEMO_ACCOUNTS = [
  { role: 'SuperAdmin', email: 'superadmin@expensetracker.local', password: 'SuperAdmin123' },
  { role: 'Admin', email: 'admin@expensetracker.local', password: 'Password123' },
  { role: 'User', email: 'user@expensetracker.local', password: 'Password123' },
  { role: 'Reader', email: 'reader@expensetracker.local', password: 'Password123' },
]

/** View: the sign-in screen (outside the app shell). */
export function LoginPage() {
  const vm = useLoginViewModel()

  if (vm.status === 'restoring') return <LoadingState label="Checking your session…" />
  if (vm.status === 'authenticated') return <Navigate to={vm.returnTo} replace />

  return (
    <div className="auth-screen">
      <section className="auth-card card" aria-labelledby="login-title">
        <div className="auth-card__brand">
          <span className="brand__mark">
            <WalletIcon size={20} />
          </span>
          <span className="brand__name">Expense Tracker</span>
          {ENV_LABEL && <span className="env-badge">{ENV_LABEL}</span>}
        </div>

        <h1 id="login-title" className="auth-card__title">Sign in</h1>
        <p className="auth-card__subtitle">Welcome back. Sign in to see your spending.</p>

        {vm.notice && !vm.formError && (
          <p className="auth-alert auth-alert--info" role="status">
            <InfoIcon size={16} /> {vm.notice}
          </p>
        )}
        {vm.formError && (
          <p className="auth-alert auth-alert--error" role="alert">
            <AlertTriangleIcon size={16} /> {vm.formError}
          </p>
        )}

        <form className="form" onSubmit={vm.submit} noValidate>
          <Field label="Email" error={vm.errors.email} htmlFor="login-email">
            <input
              id="login-email"
              className="input"
              type="email"
              autoComplete="username"
              value={vm.draft.email}
              onChange={(e) => vm.set('email', e.target.value)}
              aria-invalid={vm.errors.email ? true : undefined}
              autoFocus
            />
          </Field>

          <Field label="Password" error={vm.errors.password} htmlFor="login-password">
            <PasswordInput
              id="login-password"
              autoComplete="current-password"
              value={vm.draft.password}
              onChange={(e) => vm.set('password', e.target.value)}
              aria-invalid={vm.errors.password ? true : undefined}
            />
          </Field>

          <button type="submit" className="button button--primary auth-card__submit" disabled={vm.submitting}>
            <LockIcon size={16} /> {vm.submitting ? 'Signing in…' : 'Sign in'}
          </button>
        </form>

        <p className="muted auth-card__footnote">
          New here? <Link to="/register">Create an account</Link>
        </p>

        <SignInQrButton />

        {import.meta.env.DEV && (
          <div className="demo-accounts">
            <p className="demo-accounts__title">Development demo accounts</p>
            <div className="demo-accounts__list">
              {DEMO_ACCOUNTS.map((account) => (
                <button
                  key={account.email}
                  type="button"
                  className="button button--ghost button--sm"
                  onClick={() => vm.fillDemoAccount(account.email, account.password)}
                  title={account.email}
                >
                  {account.role}
                </button>
              ))}
            </div>
          </div>
        )}
      </section>
    </div>
  )
}
