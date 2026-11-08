import type { UserSummary } from '../model/types'

export function RoleBadges({ roles }: { roles: string[] }) {
  if (roles.length === 0) return <span className="cell-muted">No role</span>
  return (
    <span className="chips">
      {roles.map((role) => (
        <span key={role} className={`role-badge role-badge--${role.toLowerCase()}`}>
          {role}
        </span>
      ))}
    </span>
  )
}

export function UserStatus({ user }: { user: Pick<UserSummary, 'isActive' | 'isLockedOut'> }) {
  if (!user.isActive) return <span className="status-pill status-pill--revoked">Disabled</span>
  if (user.isLockedOut) return <span className="status-pill status-pill--expired">Locked</span>
  return <span className="status-pill status-pill--active">Active</span>
}
