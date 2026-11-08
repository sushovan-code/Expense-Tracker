import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { QueryRoots } from '@/shared/api/queryKeys'
import { useService } from '@/shared/di/services'
import type { CategoryInput, CategoryQuery } from '../model/types'
import { CategoriesApiToken } from './categoriesApi'

export const categoryKeys = {
  all: [QueryRoots.categories] as const,
  list: (query: CategoryQuery) => [QueryRoots.categories, 'list', query] as const,
  lookup: () => [QueryRoots.categories, 'lookup'] as const,
}

export function useCategoriesPage(query: CategoryQuery) {
  const api = useService(CategoriesApiToken)
  return useQuery({ queryKey: categoryKeys.list(query), queryFn: () => api.list(query), placeholderData: keepPreviousData })
}

/** All categories for dropdowns; rarely changes, so it's cached longer. */
export function useCategoryLookup() {
  const api = useService(CategoriesApiToken)
  return useQuery({ queryKey: categoryKeys.lookup(), queryFn: () => api.lookup(), staleTime: 5 * 60_000 })
}

/** Category commands. Names/colors show on expenses and budgets too, so those caches refresh as well. */
export function useCategoryMutations() {
  const api = useService(CategoriesApiToken)
  const queryClient = useQueryClient()
  const invalidate = () =>
    Promise.all(
      [QueryRoots.categories, QueryRoots.expenses, QueryRoots.budgets].map((root) =>
        queryClient.invalidateQueries({ queryKey: [root] }),
      ),
    )

  return {
    create: useMutation({ mutationFn: (input: CategoryInput) => api.create(input), onSuccess: invalidate }),
    update: useMutation({
      mutationFn: ({ id, input }: { id: number; input: CategoryInput }) => api.update(id, input),
      onSuccess: invalidate,
    }),
    remove: useMutation({ mutationFn: (id: number) => api.remove(id), onSuccess: invalidate }),
  }
}
