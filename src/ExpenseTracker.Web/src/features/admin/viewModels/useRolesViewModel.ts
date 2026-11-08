import { useMemo, useState } from 'react'
import { useErrorToast } from '@/shared/hooks/useErrorToast'
import { errorMessage } from '@/shared/lib/errors'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import { useAdminMutations, usePermissionCatalog, useRoles } from '../api/queries'
import type { PermissionInfo } from '../model/types'
import { useAdminAccess } from './useAdminAccess'

const sameSet = (a: string[], b: string[]) => a.length === b.length && a.every((x) => b.includes(x))

function without(drafts: Record<string, string[]>, role: string) {
  const next = { ...drafts }
  delete next[role]
  return next
}

/** ViewModel for the Roles tab: each role's permission set as an editable draft. */
export function useRolesViewModel() {
  const toast = useToast()
  const admin = useAdminAccess()
  const roles = useRoles(admin.canViewRoles)
  const catalog = usePermissionCatalog(admin.canViewRoles)
  const mutations = useAdminMutations()
  const [drafts, setDrafts] = useState<Record<string, string[]>>({})
  const [saving, setSaving] = useState<string | null>(null)
  useErrorToast(roles.error ?? undefined, "Couldn't load roles")

  const groups = useMemo(() => {
    const byGroup = new Map<string, PermissionInfo[]>()
    for (const permission of catalog.data ?? []) byGroup.set(permission.group, [...(byGroup.get(permission.group) ?? []), permission])
    return [...byGroup.entries()].map(([name, permissions]) => ({ name, permissions }))
  }, [catalog.data])

  const cards = (roles.data ?? []).map((role) => {
    const draft = drafts[role.name] ?? role.permissions
    const editable = role.canEdit && admin.canManageRoles
    return {
      role,
      selected: draft,
      editable,
      dirty: !sameSet(draft, role.permissions),
      saving: saving === role.name,
      /** You can always remove a permission, but only add ones you hold. */
      canToggle: (permission: string) => editable && (draft.includes(permission) || admin.canGrant(permission)),
      toggle: (permission: string) =>
        setDrafts((d) => {
          const current = d[role.name] ?? role.permissions
          return { ...d, [role.name]: current.includes(permission) ? current.filter((p) => p !== permission) : [...current, permission] }
        }),
      reset: () => setDrafts((d) => without(d, role.name)),
      save: async () => {
        setSaving(role.name)
        try {
          await mutations.setRolePermissions.mutateAsync({ role: role.name, permissions: draft })
          setDrafts((d) => without(d, role.name))
          logger.event('admin.role_permissions_changed', `Permissions of role ${role.name} changed`, {
            'admin.role': role.name,
            'admin.permission_count': draft.length,
          })
          toast.success(`${role.name} role updated`, {
            description: `${role.userCount} user${role.userCount === 1 ? '' : 's'} affected, effective immediately.`,
          })
        } catch (err) {
          toast.error(`Couldn't update the ${role.name} role`, { description: errorMessage(err) })
        } finally {
          setSaving(null)
        }
      },
    }
  })

  return {
    isLoading: !roles.data || !catalog.data,
    error: roles.error ?? catalog.error,
    retry: () => {
      void roles.refetch()
      void catalog.refetch()
    },
    groups,
    cards,
  }
}
