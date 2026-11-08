import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '@/shared/api/httpClient'

/**
 * Server-state cache policy. Data is considered fresh for 30 s; SignalR events invalidate it as soon
 * as anything changes (see RealtimeQuerySync), so the UI stays live without polling.
 */
export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        refetchOnWindowFocus: true,
        // Retry network blips, but not 4xx answers (they won't change on retry).
        retry: (failureCount, error) => !(error instanceof ApiError && error.status >= 400 && error.status < 500) && failureCount < 2,
      },
      mutations: { retry: false },
    },
  })
}
