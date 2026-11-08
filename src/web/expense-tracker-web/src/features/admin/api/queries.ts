import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { QueryRoots } from '@/shared/api/queryKeys'
import { useService } from '@/shared/di/services'
import type { CreateUserInput, UpdateUserInput, UserClaim, UserQuery } from '../model/types'
import { AdminApiToken } from './adminApi'

export const adminKeys = {
  all: [QueryRoots.admin] as const,
  users: (query: UserQuery) => [QueryRoots.admin, 'users', query] as const,
  user: (id: string) => [QueryRoots.admin, 'user', id] as const,
  roles: () => [QueryRoots.admin, 'roles'] as const,
  permissions: () => [QueryRoots.admin, 'permissions'] as const,
}

export function useUsersPage(query: UserQuery, enabled = true) {
  const api = useService(AdminApiToken)
  return useQuery({ queryKey: adminKeys.users(query), queryFn: () => api.listUsers(query), placeholderData: keepPreviousData, enabled })
}

export function useUser(id: string | null) {
  const api = useService(AdminApiToken)
  return useQuery({ queryKey: adminKeys.user(id ?? ''), queryFn: () => api.getUser(id!), enabled: id !== null })
}

export function useRoles(enabled = true) {
  const api = useService(AdminApiToken)
  return useQuery({ queryKey: adminKeys.roles(), queryFn: () => api.listRoles(), enabled })
}

/** The permission catalog only changes with a deployment. */
export function usePermissionCatalog(enabled = true) {
  const api = useService(AdminApiToken)
  return useQuery({ queryKey: adminKeys.permissions(), queryFn: () => api.listPermissions(), staleTime: Infinity, enabled })
}

/** Every admin change can affect lists, details, role member counts and effective permissions: refresh them all. */
export function useAdminMutations() {
  const api = useService(AdminApiToken)
  const queryClient = useQueryClient()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: adminKeys.all })
  const options = { onSuccess: invalidate }

  return {
    createUser: useMutation({ mutationFn: (input: CreateUserInput) => api.createUser(input), ...options }),
    updateUser: useMutation({ mutationFn: ({ id, input }: { id: string; input: UpdateUserInput }) => api.updateUser(id, input), ...options }),
    deleteUser: useMutation({ mutationFn: (id: string) => api.deleteUser(id), ...options }),
    setRoles: useMutation({ mutationFn: ({ id, roles }: { id: string; roles: string[] }) => api.setRoles(id, roles), ...options }),
    addClaim: useMutation({ mutationFn: ({ id, claim }: { id: string; claim: UserClaim }) => api.addClaim(id, claim), ...options }),
    removeClaim: useMutation({ mutationFn: ({ id, claim }: { id: string; claim: UserClaim }) => api.removeClaim(id, claim), ...options }),
    resetPassword: useMutation({ mutationFn: ({ id, password }: { id: string; password: string }) => api.resetPassword(id, password), ...options }),
    unlock: useMutation({ mutationFn: (id: string) => api.unlock(id), ...options }),
    revokeSessions: useMutation({ mutationFn: (id: string) => api.revokeSessions(id), ...options }),
    setRolePermissions: useMutation({
      mutationFn: ({ role, permissions }: { role: string; permissions: string[] }) => api.setRolePermissions(role, permissions),
      ...options,
    }),
  }
}
