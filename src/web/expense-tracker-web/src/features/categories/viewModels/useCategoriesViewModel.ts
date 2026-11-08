import { useCallback, useEffect, useState } from 'react'
import { Permission } from '@/shared/auth/permissions'
import { useAccess } from '@/shared/auth/useSession'
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue'
import { useErrorToast } from '@/shared/hooks/useErrorToast'
import { useUrlState } from '@/shared/hooks/useUrlState'
import { errorMessage, isConflict } from '@/shared/lib/errors'
import { formatCurrency } from '@/shared/lib/format'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import type { SortDirection } from '@/shared/types'
import { useCategoriesPage, useCategoryMutations } from '../api/queries'
import type { Category, CategoryInput, CategorySortField } from '../model/types'

const URL_DEFAULTS = { q: '', page: '1', size: '10', sort: 'name', dir: 'asc' }

const DEFAULT_DIR: Record<CategorySortField, SortDirection> = { name: 'asc', expenses: 'desc', budget: 'desc' }

/** ViewModel for the Categories screen: grid state in the URL, inline edits, create and delete commands. */
export function useCategoriesViewModel() {
  const toast = useToast()
  const access = useAccess()
  const can = {
    add: access.can(Permission.CategoryAdd),
    edit: access.can(Permission.CategoryEdit),
    delete: access.can(Permission.CategoryDelete),
  }
  const [url, setUrl] = useUrlState(URL_DEFAULTS)
  const [searchText, setSearchText] = useState(url.q)
  const debouncedSearch = useDebouncedValue(searchText, 350)
  const [creating, setCreating] = useState(false)
  const [deleting, setDeleting] = useState<Category | null>(null)

  const sortBy = url.sort as CategorySortField
  const sortDir = url.dir as SortDirection
  const query = { search: url.q.trim() || undefined, page: Number(url.page), pageSize: Number(url.size), sortBy, sortDir }

  const categories = useCategoriesPage(query)
  const mutations = useCategoryMutations()
  useErrorToast(categories.error ?? undefined, "Couldn't load categories")

  useEffect(() => {
    if (debouncedSearch !== url.q) setUrl({ q: debouncedSearch, page: '1' })
  }, [debouncedSearch, url.q, setUrl])

  const data = categories.data
  useEffect(() => {
    if (data && !categories.isPlaceholderData && data.page !== query.page) setUrl({ page: String(data.page) })
  }, [data, categories.isPlaceholderData, query.page, setUrl])

  const onSort = useCallback(
    (field: CategorySortField) => {
      const dir: SortDirection = field === sortBy ? (sortDir === 'asc' ? 'desc' : 'asc') : DEFAULT_DIR[field]
      setUrl({ sort: field, dir, page: '1' })
      logger.event('grid.sorted', `Categories sorted by ${field} ${dir}`, { 'grid.name': 'categories', 'grid.sort_by': field, 'grid.sort_dir': dir })
    },
    [sortBy, sortDir, setUrl],
  )

  const changePage = (page: number, pageSize = Number(url.size)) => {
    setUrl({ page: String(page), size: String(pageSize) })
    logger.event('grid.page_changed', `Categories page ${page} (size ${pageSize})`, { 'grid.name': 'categories', 'grid.page': page, 'grid.page_size': pageSize })
  }

  const create = async (input: CategoryInput) => {
    const saved = await mutations.create.mutateAsync(input)
    setCreating(false)
    logger.event('category.created', `Category ${saved.id} created`, {
      'category.id': saved.id, 'category.has_budget': saved.monthlyBudget != null, 'ui.source': 'dialog',
    })
    toast.success('Category created', { description: saved.name })
  }

  const saveInline = useCallback(
    async (category: Category, input: CategoryInput) => {
      const saved = await mutations.update.mutateAsync({ id: category.id, input })
      logger.event('category.updated', `Category ${saved.id} updated inline`, {
        'category.id': saved.id,
        'category.renamed': saved.name !== category.name,
        'category.budget_changed': saved.monthlyBudget !== category.monthlyBudget,
        'category.monthly_budget': saved.monthlyBudget ?? undefined,
        'ui.source': 'inline',
      })
      toast.success('Category updated', {
        description: saved.monthlyBudget != null ? `${saved.name} · budget ${formatCurrency(saved.monthlyBudget)}/month` : saved.name,
      })
    },
    [mutations.update, toast],
  )

  const deleteCategory = async (category: Category) => {
    try {
      await mutations.remove.mutateAsync(category.id)
      logger.event('category.deleted', `Category ${category.id} deleted`, { 'category.id': category.id })
      toast.success('Category deleted', { description: category.name })
    } catch (err) {
      if (isConflict(err)) {
        logger.event('category.delete_blocked', `Category ${category.id} still has expenses`, {
          'category.id': category.id, 'category.expense_count': category.expenseCount,
        }, 'warn')
        toast.warning('Category is still in use', { description: errorMessage(err) })
      } else {
        toast.error("Couldn't delete the category", { description: errorMessage(err) })
      }
    }
  }

  return {
    can,
    search: { text: searchText, setText: setSearchText, isActive: Boolean(url.q) },
    grid: {
      data,
      isLoading: !data,
      isRefreshing: categories.isFetching,
      error: categories.error,
      retry: () => void categories.refetch(),
      sortBy,
      sortDir,
      onSort,
      onPageChange: (page: number) => changePage(page),
      onPageSizeChange: (size: number) => changePage(1, size),
      onSaveRow: saveInline,
      onDeleteRow: setDeleting,
    },
    createDialog: { isOpen: creating, open: () => setCreating(true), close: () => setCreating(false), submit: create },
    deleteDialog: {
      target: deleting,
      confirm: () => (deleting ? deleteCategory(deleting) : Promise.resolve()),
      cancel: () => setDeleting(null),
    },
  }
}

export type CategoriesViewModel = ReturnType<typeof useCategoriesViewModel>
