import { useState } from 'react'
import { errorMessage } from '@/shared/lib/errors'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import { useAdminMutations, usePermissionCatalog, useUser } from '../api/queries'
import type { UpdateUserInput, UserClaim } from '../model/types'
import { useAdminAccess } from './useAdminAccess'

/** ViewModel for the user editor dialog: one user's profile, roles, claims and security actions. */
export function useUserEditorViewModel(userId: string | null, onDeleted: () => void) {
  const toast = useToast()
  const admin = useAdminAccess()
  const user = useUser(userId)
  const catalog = usePermissionCatalog()
  const mutations = useAdminMutations()
  const [confirmingDelete, setConfirmingDelete] = useState(false)

  const detail = user.data
  const blocker = detail ? admin.manageBlocker(detail) : null
  const id = userId ?? ''

  /** Runs an action with success/error toasts and a client event. Resolves to whether it worked. */
  const run = async (event: string, action: () => Promise<unknown>, titles: { done: string; failed: string }, description?: string) => {
    try {
      await action()
      logger.event(`admin.${event}`, `Admin action ${event} on user ${id}`, { 'admin.target_user_id': id })
      toast.success(titles.done, { description })
      return true
    } catch (err) {
      toast.error(titles.failed, { description: errorMessage(err) })
      return false
    }
  }

  return {
    user: detail,
    isLoading: user.isLoading,
    error: user.error,
    retry: () => void user.refetch(),
    readOnlyReason: blocker,
    canEdit: detail !== undefined && blocker === null,
    assignableRoles: admin.assignableRoles,
    /** Permission claims an admin can add: catalog entries they hold themselves. */
    grantablePermissions: (catalog.data ?? []).filter((p) => admin.canGrant(p.name)),

    /** Throws so the form can show field errors. */
    saveProfile: async (input: UpdateUserInput) => {
      await mutations.updateUser.mutateAsync({ id, input })
      logger.event('admin.user_updated', `User ${id} updated`, { 'admin.target_user_id': id, 'admin.is_active': input.isActive })
      toast.success('User updated', { description: input.isActive ? input.displayName : `${input.displayName} is disabled and signed out` })
    },
    saveRoles: (roles: string[]) =>
      run('roles_changed', () => mutations.setRoles.mutateAsync({ id, roles }),
        { done: 'Roles updated', failed: "Couldn't change the roles" }, roles.join(', ')),
    addClaim: (claim: UserClaim) =>
      run('claim_added', () => mutations.addClaim.mutateAsync({ id, claim }),
        { done: 'Claim added', failed: "Couldn't add the claim" }, `${claim.type} = ${claim.value}`),
    removeClaim: (claim: UserClaim) =>
      run('claim_removed', () => mutations.removeClaim.mutateAsync({ id, claim }),
        { done: 'Claim removed', failed: "Couldn't remove the claim" }, `${claim.type} = ${claim.value}`),
    resetPassword: (password: string) =>
      run('password_reset', () => mutations.resetPassword.mutateAsync({ id, password }),
        { done: 'Password reset', failed: "Couldn't reset the password" }, 'The user was signed out everywhere.'),
    unlock: () => run('unlocked', () => mutations.unlock.mutateAsync(id), { done: 'Account unlocked', failed: "Couldn't unlock the account" }),
    revokeSessions: () =>
      run('sessions_revoked', () => mutations.revokeSessions.mutateAsync(id),
        { done: 'Signed out everywhere', failed: "Couldn't sign the user out" }, 'Their next request will need a new sign-in.'),
    deleteDialog: {
      isOpen: confirmingDelete,
      open: () => setConfirmingDelete(true),
      cancel: () => setConfirmingDelete(false),
      confirm: async () => {
        const deleted = await run('user_deleted', () => mutations.deleteUser.mutateAsync(id),
          { done: 'User deleted', failed: "Couldn't delete the user" }, detail?.email)
        if (deleted) onDeleted()
      },
    },
  }
}

export type UserEditorViewModel = ReturnType<typeof useUserEditorViewModel>
