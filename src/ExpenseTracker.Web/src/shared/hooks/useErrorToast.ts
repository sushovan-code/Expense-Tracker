import { useEffect } from 'react'
import { useToast } from '@/shared/toast/toast'

/** Raises an error toast each time a new load error appears. */
export function useErrorToast(error: Error | undefined, title: string) {
  const toast = useToast()

  useEffect(() => {
    if (error) toast.error(title, { description: error.message })
  }, [error, title, toast])
}
