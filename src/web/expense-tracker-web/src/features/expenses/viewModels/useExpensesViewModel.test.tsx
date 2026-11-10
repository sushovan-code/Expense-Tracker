import { act, waitFor } from '@testing-library/react'
import { CategoriesApiToken, type CategoriesApi } from '@/features/categories'
import { provide } from '@/shared/di/services'
import { fakeFamiliesApi } from '@/test/fakeFamiliesApi'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { FamiliesApiToken } from '@/features/families'
import { ExpensesApiToken, type ExpensesApi } from '../api/expensesApi'
import type { Expense } from '../model/types'
import { useExpensesViewModel } from './useExpensesViewModel'

const lunch: Expense = {
  id: 'exp-7', description: 'Lunch', amount: 320, date: '2026-10-05', categoryId: 'cat-food',
  categoryName: 'Food', categoryColor: '#f97316', notes: null, createdAt: '2026-10-05T10:00:00Z', isPrivate: false, createdByUserId: 'user-1',
}

function fakeExpensesApi(): ExpensesApi {
  return {
    list: vi.fn().mockResolvedValue({ items: [lunch], page: 1, pageSize: 20, totalCount: 1, totalPages: 1, totalAmount: 320 }),
    summary: vi.fn(),
    create: vi.fn().mockImplementation(async (input) => ({ ...lunch, ...input, id: 'exp-8' })),
    update: vi.fn(),
    remove: vi.fn().mockResolvedValue(undefined),
    bulkRemove: vi.fn().mockResolvedValue({ deleted: 1, deletedIds: ['exp-7'] }),
    exportCsv: vi.fn().mockResolvedValue('expenses.csv'),
  }
}

const fakeCategoriesApi = (): CategoriesApi => ({
  list: vi.fn(),
  lookup: vi.fn().mockResolvedValue([{ id: 'cat-food', name: 'Food', color: '#f97316' }]),
  create: vi.fn(),
  update: vi.fn(),
  remove: vi.fn(),
})

function renderViewModel(route = '/expenses') {
  const expenses = fakeExpensesApi()
  const view = renderHookWithProviders(() => useExpensesViewModel(), {
    route,
    services: [provide(ExpensesApiToken, expenses), provide(CategoriesApiToken, fakeCategoriesApi()), provide(FamiliesApiToken, fakeFamiliesApi())],
  })
  return { ...view, expenses }
}

describe('useExpensesViewModel', () => {
  it('reads paging, sorting and filters from the URL', async () => {
    const { result, expenses } = renderViewModel('/expenses?category=cat-food&page=2&size=50&sort=amount&dir=asc')

    await waitFor(() => expect(result.current.grid.data).toBeDefined())
    expect(expenses.list).toHaveBeenCalledWith({
      from: undefined, to: undefined, categoryId: 'cat-food', search: undefined,
      page: 2, pageSize: 50, sortBy: 'amount', sortDir: 'asc',
    })
  })

  it('filters by family member and offers every member of the family', async () => {
    const { result, expenses } = renderViewModel('/expenses?member=user-2')

    await waitFor(() => expect(result.current.grid.data).toBeDefined())
    await waitFor(() => expect(result.current.filters.members).toHaveLength(2))

    expect(expenses.list).toHaveBeenCalledWith(expect.objectContaining({ memberId: 'user-2' }))
    expect(result.current.summary.hasFilters).toBe(true)
  })

  it('creates an expense and closes the dialog', async () => {
    const { result, expenses } = renderViewModel()
    await waitFor(() => expect(result.current.createDialog.canOpen).toBe(true))

    act(() => result.current.createDialog.open())
    await act(() => result.current.createDialog.submit({ description: 'Coffee', amount: 120, date: '2026-10-05', categoryId: 'cat-food', notes: null, isPrivate: false }))

    expect(expenses.create).toHaveBeenCalledOnce()
    expect(result.current.createDialog.isOpen).toBe(false)
  })

  it('deletes only after confirmation', async () => {
    const { result, expenses } = renderViewModel()
    await waitFor(() => expect(result.current.grid.data).toBeDefined())

    act(() => result.current.grid.onDeleteRow(lunch))
    expect(expenses.remove).not.toHaveBeenCalled()

    await act(() => result.current.deleteDialog.confirm())
    expect(expenses.remove).toHaveBeenCalledWith('exp-7')
  })

  it('bulk-deletes the selected rows after confirmation', async () => {
    const { result, expenses } = renderViewModel()
    await waitFor(() => expect(result.current.grid.data).toBeDefined())

    act(() => result.current.grid.onRowSelectionChange({ 'exp-7': true }))
    expect(result.current.selection.count).toBe(1)
    act(() => result.current.selection.requestDelete())
    expect(result.current.bulkDeleteDialog.isOpen).toBe(true)

    await act(() => result.current.bulkDeleteDialog.confirm())
    expect(expenses.bulkRemove).toHaveBeenCalledWith(['exp-7'])
    expect(result.current.selection.count).toBe(0)
  })
})
