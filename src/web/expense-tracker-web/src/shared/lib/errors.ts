import { ApiError } from '@/shared/api/httpClient'

export const errorMessage = (error: unknown) =>
  error instanceof Error ? error.message : 'Something went wrong. Please try again.'

/** Validation errors from the API, flattened to the first message per field. */
export function fieldErrorsOf(error: unknown): Record<string, string> | null {
  if (!(error instanceof ApiError) || Object.keys(error.fieldErrors).length === 0) return null
  return Object.fromEntries(Object.entries(error.fieldErrors).map(([field, messages]) => [field, messages[0]]))
}

export const isConflict = (error: unknown) => error instanceof ApiError && error.status === 409
