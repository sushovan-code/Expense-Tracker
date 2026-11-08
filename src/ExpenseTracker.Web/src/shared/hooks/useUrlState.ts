import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

/**
 * State stored in the URL query string (filters, sort, page), so views survive reloads and can be
 * shared or bookmarked. Values equal to their default are omitted to keep URLs short.
 * `defaults` must be a stable (module-level) object.
 */
export function useUrlState<T extends Record<string, string>>(defaults: T) {
  const [params, setParams] = useSearchParams()

  const state = useMemo(() => {
    const result = { ...defaults }
    for (const key of Object.keys(defaults) as (keyof T)[]) {
      const value = params.get(key as string)
      if (value !== null) result[key] = value as T[keyof T]
    }
    return result
  }, [params, defaults])

  const update = useCallback(
    (patch: Partial<T>) => {
      setParams(
        (previous) => {
          const next = new URLSearchParams(previous)
          for (const [key, value] of Object.entries(patch)) {
            if (value === undefined || value === defaults[key]) next.delete(key)
            else next.set(key, value)
          }
          return next
        },
        { replace: true },
      )
    },
    [setParams, defaults],
  )

  return [state, update] as const
}
