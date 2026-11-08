import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { QueryRoots } from '@/shared/api/queryKeys'
import { useService } from '@/shared/di/services'
import type { ApiKeyInput, ChangePasswordInput } from '../model/types'
import { AccountApiToken } from './accountApi'

const accountKeys = {
  apiKeys: () => [QueryRoots.account, 'api-keys'] as const,
}

export function useApiKeys(enabled: boolean) {
  const api = useService(AccountApiToken)
  return useQuery({ queryKey: accountKeys.apiKeys(), queryFn: () => api.listApiKeys(), enabled })
}

export function useAccountMutations() {
  const api = useService(AccountApiToken)
  const queryClient = useQueryClient()
  const invalidateKeys = () => queryClient.invalidateQueries({ queryKey: accountKeys.apiKeys() })

  return {
    changePassword: useMutation({ mutationFn: (input: ChangePasswordInput) => api.changePassword(input) }),
    createApiKey: useMutation({ mutationFn: (input: ApiKeyInput) => api.createApiKey(input), onSuccess: invalidateKeys }),
    revokeApiKey: useMutation({ mutationFn: (id: string) => api.revokeApiKey(id), onSuccess: invalidateKeys }),
  }
}
