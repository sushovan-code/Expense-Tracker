import type { ReactNode } from 'react'
import { Link, Navigate, Outlet, useLocation } from 'react-router-dom'
import type { Permission, Role } from '@/shared/auth/permissions'
import { useAccess, useSession } from '@/shared/auth/useSession'
import { ShieldIcon } from '@/shared/ui/Icons'
import { LoadingState } from '@/shared/ui/States'

interface RequireAuthProps {
  /** Also require this permission. */
  permission?: Permission
  /** Also require this role or a higher one. */
  minRole?: Role
  /** Defaults to the matched child routes. */
  children?: ReactNode
}

/**
 * Route guard. Anonymous visitors go to the login page (and come back afterwards); signed-in users
 * without the required access see an explanation instead of a page that would only fail with 403s.
 */
export function RequireAuth({ permission, minRole, children }: RequireAuthProps) {
  const { status } = useSession()
  const access = useAccess()
  const location = useLocation()

  if (status === 'restoring') return <LoadingState label="Checking your session…" />
  if (status === 'anonymous') return <Navigate to="/login" replace state={{ from: location }} />
  if ((permission && !access.can(permission)) || (minRole && !access.isAtLeast(minRole))) return <AccessDenied />

  return children ?? <Outlet />
}

export function AccessDenied() {
  return (
    <div className="state access-denied" role="alert">
      <span className="access-denied__icon">
        <ShieldIcon size={26} />
      </span>
      <p className="state__title">You don't have access to this page</p>
      <p>Ask an administrator if you need it.</p>
      <Link to="/" className="button button--ghost">
        Back to the dashboard
      </Link>
    </div>
  )
}
