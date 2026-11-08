import { buildQuery, type HttpClient } from '@/shared/api/httpClient'
import { createServiceToken } from '@/shared/di/services'
import type { PagedResponse } from '@/shared/types'
import type {
  CreateUserInput,
  PermissionInfo,
  RoleInfo,
  UpdateUserInput,
  UserClaim,
  UserDetail,
  UserQuery,
  UserSummary,
} from '../model/types'

/** User, role and claim administration (Admin or above). */
export interface AdminApi {
  listUsers(query: UserQuery): Promise<PagedResponse<UserSummary>>
  getUser(id: string): Promise<UserDetail>
  createUser(input: CreateUserInput): Promise<UserDetail>
  updateUser(id: string, input: UpdateUserInput): Promise<UserDetail>
  deleteUser(id: string): Promise<void>
  setRoles(id: string, roles: string[]): Promise<UserDetail>
  addClaim(id: string, claim: UserClaim): Promise<UserDetail>
  removeClaim(id: string, claim: UserClaim): Promise<void>
  resetPassword(id: string, newPassword: string): Promise<void>
  unlock(id: string): Promise<void>
  revokeSessions(id: string): Promise<void>
  listRoles(): Promise<RoleInfo[]>
  setRolePermissions(role: string, permissions: string[]): Promise<RoleInfo>
  listPermissions(): Promise<PermissionInfo[]>
}

export const AdminApiToken = createServiceToken<AdminApi>('AdminApi')

export function createAdminApi(http: HttpClient): AdminApi {
  const user = (id: string) => `/admin/users/${encodeURIComponent(id)}`
  return {
    listUsers: (query) => http.get('/admin/users', { ...query }),
    getUser: (id) => http.get(user(id)),
    createUser: (input) => http.post('/admin/users', input),
    updateUser: (id, input) => http.put(user(id), input),
    deleteUser: (id) => http.delete(user(id)),
    setRoles: (id, roles) => http.put(`${user(id)}/roles`, { roles }),
    addClaim: (id, claim) => http.post(`${user(id)}/claims`, claim),
    removeClaim: (id, claim) => http.delete(`${user(id)}/claims${buildQuery({ ...claim })}`),
    resetPassword: (id, newPassword) => http.post(`${user(id)}/reset-password`, { newPassword }),
    unlock: (id) => http.post(`${user(id)}/unlock`),
    revokeSessions: (id) => http.post(`${user(id)}/revoke-sessions`),
    listRoles: () => http.get('/admin/roles'),
    setRolePermissions: (role, permissions) => http.put(`/admin/roles/${encodeURIComponent(role)}/permissions`, { permissions }),
    listPermissions: () => http.get('/admin/permissions'),
  }
}
