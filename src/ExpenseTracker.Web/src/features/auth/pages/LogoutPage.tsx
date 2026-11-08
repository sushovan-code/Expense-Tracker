import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useSession } from '@/shared/auth/useSession'
import { CheckCircleIcon } from '@/shared/ui/Icons'
import { LoadingState } from '@/shared/ui/States'

/** View: signs out (revoking the server-side session) and confirms it. */
export function LogoutPage() {
  const { status, signOut } = useSession()
  const started = useRef(false)
  const [done, setDone] = useState(status === 'anonymous')

  useEffect(() => {
    // The ref guard skips StrictMode's development-only double effect run.
    if (started.current || status === 'anonymous' || status === 'restoring') return
    started.current = true
    void signOut().finally(() => setDone(true))
  }, [status, signOut])

  if (!done && status !== 'anonymous') return <LoadingState label="Signing you out…" />

  return (
    <div className="auth-screen">
      <section className="auth-card card auth-card--center" aria-labelledby="logout-title">
        <span className="auth-card__icon">
          <CheckCircleIcon size={28} />
        </span>
        <h1 id="logout-title" className="auth-card__title">You're signed out</h1>
        <p className="auth-card__subtitle">Your session on this device has ended. Other open tabs were signed out too.</p>
        <Link to="/login" className="button button--primary auth-card__submit">
          Sign in again
        </Link>
      </section>
    </div>
  )
}
