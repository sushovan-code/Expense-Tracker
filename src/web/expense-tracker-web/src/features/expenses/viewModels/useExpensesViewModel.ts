import type { RowSelectionState, Updater } from '@tanstack/react-table'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useCategoryLookup } from '@/features/categories'
import { Permission } from '@/shared/auth/permissions'
import { useAccess } from '@/shared/auth/useSession'
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue'
import { useErrorToast } from '@/shared/hooks/useErrorToast'
import { useUrlState } from '@/shared/hooks/useUrlState'
import { errorMessage } from '@/shared/lib/errors'
import { currentMonth, formatCurrency, monthRange } from '@/shared/lib/format'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import type { SortDirection } from '@/shared/types'
import { useExpenseMutations, useExpensesPage } from '../api/queries'
import type { Expense, ExpenseInput, ExpenseQuery, ExpenseSortField } from '../model/types'

/** Grid state kept in the URL (?from=&to=&category=&q=&page=&size=&sort=&dir=). */
const URL_DEFAULTS = { from: '', to: '', category: '', q: '', page: '1', size: '20', sort: 'date', dir: 'desc' }

/** Columns sort ascending first, except date/amount (newest/largest first is the natural order). */
const DEFAULT_DIR: Record<ExpenseSortField, SortDirection> = {
  date: 'desc',
  amount: 'desc',
  description: 'asc',
  category: 'asc',
  createdat: 'desc',
}

const toInput = (e: Expense): ExpenseInput => ({
  description: e.description,
  amount: e.amount,
  date: e.date,
  categoryId: e.categoryId,
  notes: e.notes,
})

/**
 * ViewModel for the Expenses screen (the "VM" in MVVM): owns screen state, derives display data and
 * exposes commands. It has no JSX — ExpensesPage only binds it to presentational components.
 */
export function useExpensesViewModel() {
  const toast = useToast()
  const access = useAccess()
  // What this user may do here. The API enforces the same permissions; these only shape the UI.
  const can = {
    add: access.can(Permission.ExpenseAdd),
    edit: access.can(Permission.ExpenseEdit),
    delete: access.can(Permission.ExpenseDelete),
    export: access.can(Permission.ExpenseExport),
  }
  const [url, setUrl] = useUrlState(URL_DEFAULTS)
  const [searchText, setSearchText] = useState(url.q)
  const debouncedSearch = useDebouncedValue(searchText, 350)
  const [creating, setCreating] = useState(false)
  const [deleting, setDeleting] = useState<Expense | null>(null)
  const [confirmingBulk, setConfirmingBulk] = useState(false)
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({})
  // Rows seen while selected, so bulk Undo can re-create them even if they were on other pages.
  const selectedRows = useRef(new Map<string, Expense>())

  const filters = useMemo(
    () => ({
      from: url.from || undefined,
      to: url.to || undefined,
      categoryId: url.category || undefined,
      search: url.q.trim() || undefined,
    }),
    [url.from, url.to, url.category, url.q],
  )
  const hasFilters = Boolean(filters.from || filters.to || filters.categoryId || filters.search)
  const sortBy = url.sort as ExpenseSortField
  const sortDir = url.dir as SortDirection
  const query: ExpenseQuery = { ...filters, page: Number(url.page), pageSize: Number(url.size), sortBy, sortDir }

  const categories = useCategoryLookup()
  const expenses = useExpensesPage(query)
  const mutations = useExpenseMutations()
  useErrorToast(categories.error ?? undefined, "Couldn't load categories")
  useErrorToast(expenses.error ?? undefined, "Couldn't load expenses")

  // Debounced search → URL (and back to page 1).
  useEffect(() => {
    if (debouncedSearch !== url.q) setUrl({ q: debouncedSearch, page: '1' })
  }, [debouncedSearch, url.q, setUrl])

  // Filters changed: log it (never the search text — it's user content) and clear the selection.
  const filterKey = JSON.stringify(filters)
  const firstFilter = useRef(true)
  useEffect(() => {
    if (firstFilter.current) {
      firstFilter.current = false
      return
    }
    setRowSelection({})
    selectedRows.current.clear()
    logger.event('expenses.filters_changed', 'Expense filters changed', {
      'filter.from': filters.from,
      'filter.to': filters.to,
      'filter.category_id': filters.categoryId,
      'filter.has_search': Boolean(filters.search),
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filterKey])

  // The server clamps out-of-range pages (e.g. after deleting the last row of the last page).
  // Placeholder data is the previous page, so only reconcile against a real response.
  const data = expenses.data
  useEffect(() => {
    if (data && !expenses.isPlaceholderData && data.page !== query.page) setUrl({ page: String(data.page) })
  }, [data, expenses.isPlaceholderData, query.page, setUrl])

  const onSort = useCallback(
    (field: ExpenseSortField) => {
      const dir: SortDirection = field === sortBy ? (sortDir === 'asc' ? 'desc' : 'asc') : DEFAULT_DIR[field]
      setUrl({ sort: field, dir, page: '1' })
      logger.event('grid.sorted', `Expenses sorted by ${field} ${dir}`, { 'grid.name': 'expenses', 'grid.sort_by': field, 'grid.sort_dir': dir })
    },
    [sortBy, sortDir, setUrl],
  )

  const changePage = (page: number, pageSize = Number(url.size)) => {
    setUrl({ page: String(page), size: String(pageSize) })
    logger.event('grid.page_changed', `Expenses page ${page} (size ${pageSize})`, { 'grid.name': 'expenses', 'grid.page': page, 'grid.page_size': pageSize })
  }

  const onRowSelectionChange = (updater: Updater<RowSelectionState>) => {
    setRowSelection((previous) => {
      const next = typeof updater === 'function' ? updater(previous) : updater
      for (const row of data?.items ?? []) {
        if (next[row.id]) selectedRows.current.set(row.id, row)
        else selectedRows.current.delete(row.id)
      }
      return next
    })
  }
  const selectedIds = Object.keys(rowSelection).filter((id) => rowSelection[id])

  // ---- Commands ------------------------------------------------------------------------------

  const create = async (input: ExpenseInput) => {
    const saved = await mutations.create.mutateAsync(input)
    setCreating(false)
    logger.event('expense.created', `Expense ${saved.id} created`, {
      'expense.id': saved.id, 'expense.amount': saved.amount, 'category.id': saved.categoryId, 'ui.source': 'dialog',
    })
    toast.success('Expense added', { description: `${saved.description} · ${formatCurrency(saved.amount)}` })
  }

  const saveInline = useCallback(
    async (expense: Expense, input: ExpenseInput) => {
      const saved = await mutations.update.mutateAsync({ id: expense.id, input })
      logger.event('expense.updated', `Expense ${saved.id} updated inline`, {
        'expense.id': saved.id,
        'expense.amount': saved.amount,
        'expense.amount_change': Math.round((saved.amount - expense.amount) * 100) / 100,
        'category.id': saved.categoryId,
        'category.changed': saved.categoryId !== expense.categoryId,
        'ui.source': 'inline',
      })
      toast.success('Expense updated', { description: `${saved.description} · ${formatCurrency(saved.amount)}` })
    },
    [mutations.update, toast],
  )

  const restore = async (items: Expense[]) => {
    try {
      await Promise.all(items.map((e) => mutations.create.mutateAsync(toInput(e))))
      logger.event('expense.restored', `${items.length} expense(s) restored via Undo`, { 'expense.count': items.length })
      toast.info(items.length === 1 ? 'Expense restored' : `${items.length} expenses restored`)
    } catch (err) {
      toast.error("Couldn't restore", { description: errorMessage(err) })
    }
  }

  const deleteOne = async (expense: Expense) => {
    try {
      await mutations.remove.mutateAsync(expense.id)
      logger.event('expense.deleted', `Expense ${expense.id} deleted`, { 'expense.id': expense.id, 'expense.amount': expense.amount })
      toast.success('Expense deleted', {
        description: expense.description,
        action: can.add ? { label: 'Undo', onClick: () => void restore([expense]) } : undefined,
      })
    } catch (err) {
      toast.error("Couldn't delete the expense", { description: errorMessage(err) })
    }
  }

  const deleteSelected = async () => {
    const known = selectedIds.map((id) => selectedRows.current.get(id)).filter((e): e is Expense => Boolean(e))
    try {
      const result = await mutations.bulkRemove.mutateAsync(selectedIds)
      setRowSelection({})
      selectedRows.current.clear()
      logger.event('expenses.bulk_deleted', `${result.deleted} expenses deleted in bulk`, {
        'expense.count': result.deleted, 'expense.requested_count': selectedIds.length,
      })
      const restorable = known.filter((e) => result.deletedIds.includes(e.id))
      toast.success(`${result.deleted} expense${result.deleted === 1 ? '' : 's'} deleted`, {
        description: result.deleted < selectedIds.length ? `${selectedIds.length - result.deleted} were already gone.` : undefined,
        action: can.add && restorable.length > 0 ? { label: 'Undo', onClick: () => void restore(restorable) } : undefined,
      })
    } catch (err) {
      toast.error("Couldn't delete the selected expenses", { description: errorMessage(err) })
    }
  }

  const exportCsv = async () => {
    try {
      const fileName = await mutations.exportCsv.mutateAsync({ ...filters, sortBy, sortDir })
      logger.event('expenses.exported', `Exported ${data?.totalCount ?? 0} expenses to CSV`, {
        'export.row_count': data?.totalCount ?? 0,
        'filter.active': hasFilters,
      })
      toast.success('Export ready', { description: fileName })
    } catch (err) {
      toast.error("Couldn't export", { description: errorMessage(err) })
    }
  }

  return {
    can,
    summary: {
      totalCount: data?.totalCount,
      totalAmount: data?.totalAmount,
      hasFilters,
    },
    filters: {
      searchText,
      setSearchText,
      from: url.from,
      to: url.to,
      categoryId: url.category,
      categories: categories.data ?? [],
      setFrom: (from: string) => setUrl({ from, page: '1' }),
      setTo: (to: string) => setUrl({ to, page: '1' }),
      setCategory: (category: string) => setUrl({ category, page: '1' }),
      showThisMonth: () => {
        setSearchText('')
        setUrl({ ...monthRange(currentMonth()), category: '', q: '', page: '1' })
      },
      clear: () => {
        setSearchText('')
        setUrl({ from: '', to: '', category: '', q: '', page: '1' })
      },
    },
    grid: {
      data,
      categories: categories.data,
      isLoading: !data || !categories.data,
      isRefreshing: expenses.isFetching,
      error: expenses.error,
      retry: () => void expenses.refetch(),
      sortBy,
      sortDir,
      onSort,
      onPageChange: (page: number) => changePage(page),
      onPageSizeChange: (size: number) => changePage(1, size),
      rowSelection,
      onRowSelectionChange,
      onSaveRow: saveInline,
      onDeleteRow: setDeleting,
    },
    selection: {
      count: selectedIds.length,
      spansPages: selectedIds.some((id) => !data?.items.some((e) => e.id === id)),
      clear: () => onRowSelectionChange({}),
      requestDelete: () => setConfirmingBulk(true),
    },
    createDialog: {
      isOpen: creating,
      canOpen: Boolean(categories.data),
      open: () => setCreating(true),
      close: () => setCreating(false),
      submit: create,
    },
    deleteDialog: {
      target: deleting,
      confirm: () => (deleting ? deleteOne(deleting) : Promise.resolve()),
      cancel: () => setDeleting(null),
    },
    bulkDeleteDialog: {
      isOpen: confirmingBulk,
      count: selectedIds.length,
      confirm: deleteSelected,
      cancel: () => setConfirmingBulk(false),
    },
    exporting: {
      isBusy: mutations.exportCsv.isPending,
      canExport: Boolean(data?.totalCount),
      run: exportCsv,
    },
  }
}

export type ExpensesViewModel = ReturnType<typeof useExpensesViewModel>
