import { useState } from 'react'
import { PUBLIC_URL } from '@/shared/config/env'
import { switchActiveFamily } from '@/shared/family/activeFamily'
import { useErrorToast } from '@/shared/hooks/useErrorToast'
import { errorMessage } from '@/shared/lib/errors'
import { useSession } from '@/shared/auth/useSession'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import { useFamilyMutations, useMyFamily } from '../api/queries'
import type { AddMemberInput, CreatedInvite, FamilyMember, FamilyRole } from '../model/types'

/** Where an invite token leads: the accept-invite page of this site. */
export const inviteLink = (token: string, baseUrl: string = PUBLIC_URL) => `${baseUrl}/accept-invite?token=${encodeURIComponent(token)}`

/**
 * ViewModel for the Family screen: loads the family, derives what the signed-in person may do
 * (the API enforces the same rules) and exposes the commands. No JSX.
 */
export function useFamilyViewModel() {
  const toast = useToast()
  const { user } = useSession()
  const query = useMyFamily()
  const mutations = useFamilyMutations()
  useErrorToast(query.error ?? undefined, "Couldn't load your family")

  const [addOpen, setAddOpen] = useState(false)
  const [newFamilyOpen, setNewFamilyOpen] = useState(false)
  const [inviteOpen, setInviteOpen] = useState(false)
  const [createdInvite, setCreatedInvite] = useState<CreatedInvite | null>(null)
  const [removing, setRemoving] = useState<FamilyMember | null>(null)
  const [leaving, setLeaving] = useState(false)

  const family = query.data
  const isOwner = family?.myRole === 'owner'

  /** Runs a command and shows a toast; for commands that have no form of their own to show errors in. */
  const run = async (action: () => Promise<unknown>, success: string, failure: string) => {
    try {
      await action()
      toast.success(success)
    } catch (err) {
      toast.error(failure, { description: errorMessage(err) })
    }
  }

  // Create and add-member throw on failure so their forms can show field errors.
  const createFamily = async (name: string) => {
    const created = await mutations.create.mutateAsync(name)
    logger.event('family.created', `Family ${created.id} created`, { 'family.id': created.id })
    toast.success('Family created', { description: created.name })
    // Work in the new family from now on (reloads, which also joins its live-update group).
    switchActiveFamily(created.id, '/family')
  }

  const rename = async (name: string) => {
    await mutations.rename.mutateAsync(name)
    toast.success('Family renamed')
  }

  const addMember = async (input: AddMemberInput) => {
    await mutations.addMember.mutateAsync(input)
    setAddOpen(false)
    toast.success('Member added', { description: input.displayName })
  }

  const createInvite = (role: FamilyRole) =>
    run(async () => setCreatedInvite(await mutations.createInvite.mutateAsync(role)), 'Invite link created', "Couldn't create the invite")

  return {
    user,
    isLoading: query.isPending,
    error: query.error,
    retry: () => void query.refetch(),
    /** `null` when the person has no family yet. */
    family,
    can: { manage: isOwner },
    createFamily,
    rename,
    changeRole: (member: FamilyMember, role: FamilyRole) =>
      run(() => mutations.changeRole.mutateAsync({ userId: member.userId, role }), 'Role updated', "Couldn't change the role"),
    revokeInvite: (id: string) => run(() => mutations.revokeInvite.mutateAsync(id), 'Invite revoked', "Couldn't revoke the invite"),
    newFamilyDialog: { isOpen: newFamilyOpen, open: () => setNewFamilyOpen(true), close: () => setNewFamilyOpen(false) },
    addDialog: { isOpen: addOpen, open: () => setAddOpen(true), close: () => setAddOpen(false), submit: addMember },
    inviteDialog: {
      isOpen: inviteOpen,
      created: createdInvite,
      open: () => setInviteOpen(true),
      close: () => {
        setInviteOpen(false)
        setCreatedInvite(null)
      },
      create: createInvite,
    },
    removeDialog: {
      target: removing,
      open: setRemoving,
      cancel: () => setRemoving(null),
      confirm: () =>
        removing
          ? run(() => mutations.removeMember.mutateAsync(removing.userId), 'Member removed', "Couldn't remove the member")
          : Promise.resolve(),
    },
    leaveDialog: {
      isOpen: leaving,
      open: () => setLeaving(true),
      cancel: () => setLeaving(false),
      confirm: () => run(() => mutations.leave.mutateAsync(), 'You left the family', "Couldn't leave the family"),
    },
  }
}

export type FamilyViewModel = ReturnType<typeof useFamilyViewModel>
