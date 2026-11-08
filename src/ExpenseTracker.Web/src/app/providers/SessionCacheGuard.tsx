import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'
import { useSession } from '@/shared/auth/useSession'

/**
 * Drops all cached server data whenever the signed-in user changes (sign-out, or another account
 * signing in from another tab), so one user never sees another's cached responses.
 */
export function SessionCacheGuard() {
  const { status, user } = useSession()
  const queryClient = useQueryClient()
  const previous = useRef<string | null | undefined>(undefined)

  useEffect(() => {
    if (status === 'restoring') return
    const current = user?.id ?? null
    if (previous.current !== undefined && previous.current !== current) queryClient.clear()
    previous.current = current
  }, [status, user, queryClient])

  return null
}
