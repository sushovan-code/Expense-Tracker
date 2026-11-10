import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '@/shared/api/httpClient'
import { QueryRoots } from '@/shared/api/queryKeys'
import { useService } from '@/shared/di/services'
import type { AcceptInviteInput, AddMemberInput, FamilyRole } from '../model/types'
import { FamiliesApiToken } from './familiesApi'

export const familyKeys = {
  all: [QueryRoots.family] as const,
  mine: () => [QueryRoots.family, 'mine'] as const,
}

const HTTP_FORBIDDEN = 403

/** The signed-in user's family; `null` (not an error) when they don't belong to one yet. */
export function useMyFamily() {
  const api = useService(FamiliesApiToken)
  return useQuery({
    queryKey: familyKeys.mine(),
    queryFn: async () => {
      try {
        return await api.getMine()
      } catch (error) {
        if (error instanceof ApiError && error.status === HTTP_FORBIDDEN) return null
        throw error
      }
    },
    staleTime: 60_000,
  })
}

/** Members of the signed-in user's family, for member filters. Empty while loading or without a family. */
export function useFamilyMembers() {
  const family = useMyFamily()
  return family.data?.members ?? []
}

/**
 * Family commands. Membership and roles change what the user can see everywhere (expenses, categories,
 * budgets), so every cache is refreshed after a change.
 */
export function useFamilyMutations() {
  const api = useService(FamiliesApiToken)
  const queryClient = useQueryClient()
  const refreshAll = () => queryClient.invalidateQueries()

  return {
    create: useMutation({ mutationFn: (name: string) => api.create(name), onSuccess: refreshAll }),
    rename: useMutation({ mutationFn: (name: string) => api.rename(name), onSuccess: refreshAll }),
    addMember: useMutation({ mutationFn: (input: AddMemberInput) => api.addMember(input), onSuccess: refreshAll }),
    changeRole: useMutation({
      mutationFn: ({ userId, role }: { userId: string; role: FamilyRole }) => api.changeRole(userId, role),
      onSuccess: refreshAll,
    }),
    removeMember: useMutation({ mutationFn: (userId: string) => api.removeMember(userId), onSuccess: refreshAll }),
    leave: useMutation({ mutationFn: () => api.leave(), onSuccess: refreshAll }),
    createInvite: useMutation({ mutationFn: (role: FamilyRole) => api.createInvite(role), onSuccess: refreshAll }),
    revokeInvite: useMutation({ mutationFn: (id: string) => api.revokeInvite(id), onSuccess: refreshAll }),
    join: useMutation({ mutationFn: (token: string) => api.join(token), onSuccess: refreshAll }),
    acceptInvite: useMutation({ mutationFn: (input: AcceptInviteInput) => api.acceptInvite(input) }),
  }
}
