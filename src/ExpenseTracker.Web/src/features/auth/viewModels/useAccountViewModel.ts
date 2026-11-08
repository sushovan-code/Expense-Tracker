import { useState } from 'react'
import { Permission } from '@/shared/auth/permissions'
import { useAccess, useSession } from '@/shared/auth/useSession'
import { useErrorToast } from '@/shared/hooks/useErrorToast'
import { errorMessage } from '@/shared/lib/errors'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import { useAccountMutations, useApiKeys } from '../api/queries'
import type { ApiKey, ApiKeyInput, ChangePasswordInput, CreatedApiKey } from '../model/types'

/** ViewModel for the Account screen: profile, password change and personal API keys. */
export function useAccountViewModel() {
  const { user } = useSession()
  const access = useAccess()
  const toast = useToast()
  const canManageKeys = access.can(Permission.ApiKeyManage)

  const keys = useApiKeys(canManageKeys)
  const mutations = useAccountMutations()
  useErrorToast(keys.error ?? undefined, "Couldn't load your API keys")

  const [creating, setCreating] = useState(false)
  const [revealed, setRevealed] = useState<CreatedApiKey | null>(null)
  const [revoking, setRevoking] = useState<ApiKey | null>(null)

  /** Throws (e.g. ApiError with field errors) so the form can show them. */
  const changePassword = async (input: ChangePasswordInput) => {
    await mutations.changePassword.mutateAsync(input)
    logger.event('account.password_changed', 'Password changed')
    toast.success('Password changed', { description: 'Your other sessions were signed out.' })
  }

  const createKey = async (input: ApiKeyInput) => {
    const created = await mutations.createApiKey.mutateAsync(input)
    setCreating(false)
    setRevealed(created)
    logger.event('account.api_key_created', 'API key created', {
      'api_key.id': created.apiKey.id,
      'api_key.scoped': created.apiKey.scopes.length > 0,
      'api_key.expires_in_days': input.expiresInDays,
    })
  }

  const revokeKey = async (key: ApiKey) => {
    try {
      await mutations.revokeApiKey.mutateAsync(key.id)
      logger.event('account.api_key_revoked', 'API key revoked', { 'api_key.id': key.id })
      toast.success('API key revoked', { description: `${key.name} (${key.prefix}…) no longer works.` })
    } catch (err) {
      toast.error("Couldn't revoke the key", { description: errorMessage(err) })
    }
  }

  return {
    user,
    changePassword,
    apiKeys: {
      enabled: canManageKeys,
      data: keys.data,
      isLoading: keys.isLoading,
      error: keys.error,
      retry: () => void keys.refetch(),
      /** A key can only be limited to permissions its owner holds. */
      grantable: user?.permissions ?? [],
    },
    createDialog: { isOpen: creating, open: () => setCreating(true), close: () => setCreating(false), submit: createKey },
    revealDialog: { created: revealed, close: () => setRevealed(null) },
    revokeDialog: {
      target: revoking,
      request: setRevoking,
      confirm: () => (revoking ? revokeKey(revoking) : Promise.resolve()),
      cancel: () => setRevoking(null),
    },
  }
}
