import type { HttpClient } from '@/shared/api/httpClient'
import { createServiceToken } from '@/shared/di/services'
import type { ApiKey, ApiKeyInput, ChangePasswordInput, CreatedApiKey } from '../model/types'

/** Self-service account operations. Sign-in/out and token renewal live in the AuthSession (shared/auth). */
export interface AccountApi {
  changePassword(input: ChangePasswordInput): Promise<void>
  listApiKeys(): Promise<ApiKey[]>
  createApiKey(input: ApiKeyInput): Promise<CreatedApiKey>
  revokeApiKey(id: string): Promise<void>
}

export const AccountApiToken = createServiceToken<AccountApi>('AccountApi')

export function createAccountApi(http: HttpClient): AccountApi {
  return {
    changePassword: (input) => http.post('/auth/change-password', input),
    listApiKeys: () => http.get('/auth/api-keys'),
    createApiKey: (input) => http.post('/auth/api-keys', input),
    revokeApiKey: (id) => http.delete(`/auth/api-keys/${id}`),
  }
}
