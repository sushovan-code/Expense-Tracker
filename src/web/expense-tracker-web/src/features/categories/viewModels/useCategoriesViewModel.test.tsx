import { act, waitFor } from '@testing-library/react'
import { ApiError } from '@/shared/api/httpClient'
import { provide } from '@/shared/di/services'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { CategoriesApiToken, type CategoriesApi } from '../api/categoriesApi'
import type { Category } from '../model/types'
import { useCategoriesViewModel } from './useCategoriesViewModel'

const food: Category = { id: 'cat-food', name: 'Food', color: '#f97316', monthlyBudget: null, expenseCount: 2 }

/** Fake service injected through DI — no HTTP mocking needed. */
function fakeApi(overrides: Partial<CategoriesApi> = {}): CategoriesApi {
  return {
    list: vi.fn().mockResolvedValue({ items: [food], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 }),
    lookup: vi.fn().mockResolvedValue([]),
    create: vi.fn(),
    update: vi.fn().mockImplementation(async (id, input) => ({ ...food, id, ...input })),
    remove: vi.fn().mockResolvedValue(undefined),
    ...overrides,
  }
}

const renderViewModel = (api: CategoriesApi) =>
  renderHookWithProviders(() => useCategoriesViewModel(), { services: [provide(CategoriesApiToken, api)] })

describe('useCategoriesViewModel', () => {
  it('loads the first page sorted by name', async () => {
    const api = fakeApi()
    const { result } = renderViewModel(api)

    await waitFor(() => expect(result.current.grid.data?.items).toEqual([food]))
    expect(api.list).toHaveBeenCalledWith({ search: undefined, page: 1, pageSize: 10, sortBy: 'name', sortDir: 'asc' })
  })

  it('toggles the sort direction and goes back to page 1', async () => {
    const api = fakeApi()
    const { result } = renderViewModel(api)
    await waitFor(() => expect(result.current.grid.data).toBeDefined())

    act(() => result.current.grid.onSort('name'))

    await waitFor(() => expect(result.current.grid.sortDir).toBe('desc'))
    expect(api.list).toHaveBeenLastCalledWith(expect.objectContaining({ sortBy: 'name', sortDir: 'desc', page: 1 }))
  })

  it('saves an inline edit through the service', async () => {
    const api = fakeApi()
    const { result } = renderViewModel(api)
    await waitFor(() => expect(result.current.grid.data).toBeDefined())

    await act(() => result.current.grid.onSaveRow(food, { name: 'Groceries', color: '#f97316', monthlyBudget: 5000 }))

    expect(api.update).toHaveBeenCalledWith('cat-food', { name: 'Groceries', color: '#f97316', monthlyBudget: 5000 })
  })

  it('asks for confirmation before deleting, and handles "still in use" gracefully', async () => {
    const api = fakeApi({ remove: vi.fn().mockRejectedValue(new ApiError('"Food" still has 2 expense(s).', 409)) })
    const { result } = renderViewModel(api)
    await waitFor(() => expect(result.current.grid.data).toBeDefined())

    act(() => result.current.grid.onDeleteRow(food))
    expect(result.current.deleteDialog.target).toBe(food)
    expect(api.remove).not.toHaveBeenCalled()

    await act(() => result.current.deleteDialog.confirm())
    expect(api.remove).toHaveBeenCalledWith('cat-food')
  })
})
