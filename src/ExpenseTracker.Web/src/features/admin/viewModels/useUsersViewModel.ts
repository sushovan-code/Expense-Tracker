import { useCallback, useEffect, useState } from 'react'
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue'
import { useErrorToast } from '@/shared/hooks/useErrorToast'
import { useUrlState } from '@/shared/hooks/useUrlState'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import type { SortDirection } from '@/shared/types'
import { useAdminMutations, useUsersPage } from '../api/queries'
import type { CreateUserInput, UserSortField } from '../model/types'
import { useAdminAccess } from './useAdminAccess'

const URL_DEFAULTS = { q: '', role: '', page: '1', size: '10', sort: 'name', dir: 'asc' }

const DEFAULT_DIR: Record<UserSortField, SortDirection> = { name: 'asc', email: 'asc', created: 'desc', lastlogin: 'desc' }

/** ViewModel for the Users tab: grid state in the URL, plus create and open-editor commands. */
export function useUsersViewModel() {
  const toast = useToast()
  const admin = useAdminAccess()
  const [url, setUrl] = useUrlState(URL_DEFAULTS)
  const [searchText, setSearchText] = useState(url.q)
  const debouncedSearch = useDebouncedValue(searchText, 350)
  const [creating, setCreating] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const sortBy = url.sort as UserSortField
  const sortDir = url.dir as SortDirection
  const query = {
    search: url.q.trim() || undefined,
    role: url.role || undefined,
    page: Number(url.page),
    pageSize: Number(url.size),
    sortBy,
    sortDir,
  }

  const users = useUsersPage(query, admin.canViewUsers)
  const mutations = useAdminMutations()
  useErrorToast(users.error ?? undefined, "Couldn't load users")

  useEffect(() => {
    if (debouncedSearch !== url.q) setUrl({ q: debouncedSearch, page: '1' })
  }, [debouncedSearch, url.q, setUrl])

  const data = users.data
  useEffect(() => {
    if (data && !users.isPlaceholderData && data.page !== query.page) setUrl({ page: String(data.page) })
  }, [data, users.isPlaceholderData, query.page, setUrl])

  const onSort = useCallback(
    (field: UserSortField) => {
      const dir: SortDirection = field === sortBy ? (sortDir === 'asc' ? 'desc' : 'asc') : DEFAULT_DIR[field]
      setUrl({ sort: field, dir, page: '1' })
    },
    [sortBy, sortDir, setUrl],
  )

  const create = async (input: CreateUserInput) => {
    const created = await mutations.createUser.mutateAsync(input)
    setCreating(false)
    logger.event('admin.user_created', `User ${created.id} created`, { 'admin.target_user_id': created.id, 'admin.roles': created.roles.join(',') })
    toast.success('User created', { description: `${created.displayName} · ${created.roles.join(', ')}` })
  }

  return {
    access: admin,
    search: { text: searchText, setText: setSearchText, isActive: Boolean(url.q || url.role) },
    roleFilter: { value: url.role, set: (role: string) => setUrl({ role, page: '1' }) },
    grid: {
      data,
      isLoading: !data,
      isRefreshing: users.isFetching,
      error: users.error,
      retry: () => void users.refetch(),
      sortBy,
      sortDir,
      onSort,
      onPageChange: (page: number) => setUrl({ page: String(page) }),
      onPageSizeChange: (size: number) => setUrl({ page: '1', size: String(size) }),
      onOpen: setEditingId,
    },
    createDialog: { canOpen: admin.canManageUsers, isOpen: creating, open: () => setCreating(true), close: () => setCreating(false), submit: create },
    editor: { userId: editingId, close: () => setEditingId(null) },
  }
}
