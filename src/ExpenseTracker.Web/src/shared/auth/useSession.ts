import { useMemo, useSyncExternalStore } from 'react'
import { createServiceToken, useService } from '@/shared/di/services'
import { highestRank, roleRank, type Permission, type Role } from './permissions'
import type { AuthSession, SessionState } from './session'

export const AuthSessionToken = createServiceToken<AuthSession>('AuthSession')

/** The current session state plus sign-in/sign-out commands. Re-renders when the session changes. */
export function useSession(): SessionState & Pick<AuthSession, 'signIn' | 'signOut'> {
  const session = useService(AuthSessionToken)
  const state = useSyncExternalStore(session.subscribe, session.getState)
  return { ...state, signIn: session.signIn, signOut: session.signOut }
}

export interface Access {
  /** Whether the signed-in user holds the permission. */
  can(permission: Permission): boolean
  /** Whether the user's highest role is `role` or above (e.g. "Admin or above"). */
  isAtLeast(role: Role): boolean
}

/**
 * What the signed-in user may do, for showing or hiding UI. Purely cosmetic: the API checks the same
 * permissions on every request, so hiding a button is never the only line of defence.
 */
export function useAccess(): Access {
  const { user } = useSession()
  return useMemo(() => {
    const permissions = new Set(user?.permissions ?? [])
    const rank = highestRank(user?.roles ?? [])
    return {
      can: (permission) => permissions.has(permission),
      isAtLeast: (role) => rank > 0 && rank >= roleRank(role),
    }
  }, [user])
}
