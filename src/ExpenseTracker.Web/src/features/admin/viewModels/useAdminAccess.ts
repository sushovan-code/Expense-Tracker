import { useMemo } from 'react'
import { highestRank, Permission, Role, roleRank } from '@/shared/auth/permissions'
import { useAccess, useSession } from '@/shared/auth/useSession'

export const ROLE_ORDER: string[] = [Role.SuperAdmin, Role.Admin, Role.User, Role.Reader]

/**
 * The signed-in administrator's powers, mirroring the server's AccessPolicy so the UI only offers
 * what will succeed: you manage users strictly below your rank (SuperAdmins: everyone), never yourself,
 * assign only roles below your own, and grant only permissions you hold.
 */
export function useAdminAccess() {
  const { user } = useSession()
  const access = useAccess()

  return useMemo(() => {
    const roles = user?.roles ?? []
    const isSuperAdmin = roles.includes(Role.SuperAdmin)
    const rank = highestRank(roles)
    const held = new Set(user?.permissions ?? [])

    return {
      userId: user?.id,
      canViewUsers: access.can(Permission.UserView),
      canManageUsers: access.can(Permission.UserManage),
      canViewRoles: access.can(Permission.RoleView),
      canManageRoles: access.can(Permission.RoleManage),
      assignableRoles: ROLE_ORDER.filter((role) => isSuperAdmin || roleRank(role) < rank),
      canGrant: (permission: string) => held.has(permission),
      /** Why the target can't be managed, or null when they can. */
      manageBlocker(target: { id: string; roles: string[] }): string | null {
        if (!access.can(Permission.UserManage)) return "You don't have permission to manage users."
        if (target.id === user?.id) return 'This is your own account. Use the Account page to change your password or API keys.'
        if (!isSuperAdmin && highestRank(target.roles) >= rank) return 'This user has the same or a higher role than you, so you can view but not change them.'
        return null
      },
    }
  }, [user, access])
}

export type AdminAccess = ReturnType<typeof useAdminAccess>
