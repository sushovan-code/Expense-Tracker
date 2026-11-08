import type { UserSummary } from '../model/types'

export function UserStatus({ user }: { user: Pick<UserSummary, 'isActive' | 'isLockedOut'> }) {
  if (!user.isActive) return <span className="status-pill status-pill--revoked">Disabled</span>
  if (user.isLockedOut) return <span className="status-pill status-pill--expired">Locked</span>
  return <span className="status-pill status-pill--active">Active</span>
}
