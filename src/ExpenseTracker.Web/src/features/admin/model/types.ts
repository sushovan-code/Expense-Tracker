import type { SortDirection } from '@/shared/types'

export interface UserSummary {
  id: string
  email: string
  displayName: string
  roles: string[]
  isActive: boolean
  isLockedOut: boolean
  createdAt: string
  lastLoginAt: string | null
}

export interface UserClaim {
  type: string
  value: string
}

export interface UserDetail extends UserSummary {
  lockoutEnd: string | null
  accessFailedCount: number
  claims: UserClaim[]
  /** Everything the user can do: role permissions plus direct permission claims. */
  effectivePermissions: string[]
}

export type UserSortField = 'name' | 'email' | 'created' | 'lastlogin'

export interface UserQuery {
  search?: string
  role?: string
  page: number
  pageSize: number
  sortBy: UserSortField
  sortDir: SortDirection
}

export interface CreateUserInput {
  email: string
  displayName: string
  password: string
  roles: string[]
}

export interface UpdateUserInput {
  email: string
  displayName: string
  isActive: boolean
}

export interface RoleInfo {
  name: string
  rank: number
  description: string
  permissions: string[]
  userCount: number
  /** Whether the signed-in administrator may change this role's permissions. */
  canEdit: boolean
}

export interface PermissionInfo {
  name: string
  group: string
  description: string
}

export const PERMISSION_CLAIM = 'permission'
