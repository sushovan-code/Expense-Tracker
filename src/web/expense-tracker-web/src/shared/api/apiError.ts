/** Error from the API (RFC 9457 problem details), with field errors keyed by camelCase field name. */
export class ApiError extends Error {
  readonly status: number
  readonly title: string | undefined
  readonly fieldErrors: Record<string, string[]>

  constructor(message: string, status: number, fieldErrors: Record<string, string[]> = {}, title?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
    this.title = title
  }
}

export interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

const camelCase = (key: string) => key.charAt(0).toLowerCase() + key.slice(1)

/** Reads a failed response's problem details (if any) into an ApiError. */
export async function toApiError(response: Response): Promise<{ error: ApiError; problem: ProblemDetails }> {
  let problem: ProblemDetails = {}
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    // Not a problem-details body.
  }
  const fieldErrors = Object.fromEntries(
    Object.entries(problem.errors ?? {}).map(([key, messages]) => [camelCase(key), messages]),
  )
  const firstFieldError = Object.values(fieldErrors)[0]?.[0]
  // Framework 401/403/429 responses carry only a generic title; say what it means instead.
  const message = problem.detail ?? firstFieldError ?? STATUS_MESSAGES[response.status] ?? problem.title ?? `Request failed (${response.status})`
  return { error: new ApiError(message, response.status, fieldErrors, problem.title), problem }
}

const STATUS_MESSAGES: Record<number, string> = {
  401: 'Your session has expired. Please sign in again.',
  403: "You don't have permission to do that.",
  429: 'Too many attempts. Please wait a minute and try again.',
}

export const unreachable = () => new ApiError('Could not reach the server. Is the API running?', 0)
