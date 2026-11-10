import { context, SpanStatusCode, trace, type Context, type Span } from '@opentelemetry/api'
import { createServiceToken } from '@/shared/di/services'
import { FAMILY_HEADER, getActiveFamilyId } from '@/shared/family/activeFamily'
import { logger } from '@/shared/telemetry'
import { toApiError, unreachable } from './apiError'

export { ApiError } from './apiError'

export type QueryParams = Record<string, string | number | boolean | undefined | null>

/** Transport used by every feature API. Features depend on this interface, never on fetch. */
export interface HttpClient {
  get<T>(path: string, query?: QueryParams): Promise<T>
  post<T>(path: string, body?: unknown): Promise<T>
  put<T>(path: string, body: unknown): Promise<T>
  delete(path: string): Promise<void>
  /** Downloads a file response and saves it in the browser; returns the file name. */
  download(path: string, query: QueryParams | undefined, fallbackName: string): Promise<string>
}

export const HttpClientToken = createServiceToken<HttpClient>('HttpClient')

/** How the client authenticates. Kept structural so the transport doesn't depend on the session implementation. */
export interface HttpAuth {
  /** Current access token, sent as `Authorization: Bearer`. */
  getAccessToken(): string | null
  /** Renews the access token; resolves true when a new one is available. */
  refresh(): Promise<boolean>
}

export interface HttpClientOptions {
  /** e.g. `/api` (same origin) or `https://api.example.com/api`. */
  baseUrl: string
  /** Sent as X-Client-Id so the server can correlate logs and skip realtime echoes. */
  clientId: string
  auth?: HttpAuth
  fetch?: typeof fetch
}

export function buildQuery(params: QueryParams = {}): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  }
  const query = search.toString()
  return query ? `?${query}` : ''
}

const SLOW_CALL_MS = 1500
const tracer = trace.getTracer('expense-tracker-client')

/**
 * fetch-based HttpClient. Every call gets its own span (the fetch instrumentation's HTTP span and the
 * API's server/SQL spans become its children) and its logs are bound to that span.
 * With `auth`, calls carry the access token, and a 401 triggers one token refresh and a single retry.
 */
export function createHttpClient({ baseUrl, clientId, auth, fetch: fetchImpl = globalThis.fetch.bind(globalThis) }: HttpClientOptions): HttpClient {
  const baseHeaders = { Accept: 'application/json', 'X-Client-Id': clientId }

  const headersFor = (body: unknown): Record<string, string> => {
    const token = auth?.getAccessToken()
    const familyId = getActiveFamilyId()
    return {
      ...baseHeaders,
      ...(familyId ? { [FAMILY_HEADER]: familyId } : {}),
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    }
  }

  async function send(method: string, path: string, query?: QueryParams, body?: unknown, isRetry = false): Promise<Response> {
    const route = path
    // No query string in telemetry: it can contain user-entered search text.
    const attributes = { 'http.request.method': method, 'url.path': `${baseUrl}${route}` }
    const span: Span = tracer.startSpan(`API ${method} ${route}`, { attributes })
    const spanContext: Context = trace.setSpan(context.active(), span)
    const log = logger.withContext(spanContext)
    const started = performance.now()

    try {
      let response: Response
      try {
        response = await context.with(spanContext, () =>
          fetchImpl(`${baseUrl}${path}${buildQuery(query)}`, {
            method,
            headers: headersFor(body),
            body: body === undefined ? undefined : JSON.stringify(body),
          }),
        )
      } catch (error) {
        span.setStatus({ code: SpanStatusCode.ERROR, message: 'Server unreachable' })
        log.error(`API ${method} ${route} failed: server unreachable`, error, attributes)
        throw unreachable()
      }

      const durationMs = Math.round(performance.now() - started)
      span.setAttribute('http.response.status_code', response.status)

      // Expired or invalidated access token: renew it once, then replay the call.
      if (response.status === 401 && auth && !isRetry) {
        span.setAttribute('auth.refresh_attempted', true)
        if (await auth.refresh()) return await send(method, path, query, body, true)
      }

      if (!response.ok) {
        const { error, problem } = await toApiError(response)

        // 4xx are expected outcomes (validation, not found, conflict); 5xx are failures.
        const message = `API ${method} ${route} returned ${response.status}`
        const problemAttributes = {
          ...attributes,
          'http.response.status_code': response.status,
          'http.client.duration_ms': durationMs,
          'problem.title': problem.title,
          'problem.detail': problem.detail,
          'problem.fields': Object.keys(error.fieldErrors).join(',') || undefined,
        }
        if (response.status >= 500) {
          span.setStatus({ code: SpanStatusCode.ERROR, message })
          log.error(message, undefined, problemAttributes)
        } else {
          log.warn(message, problemAttributes)
        }

        throw error
      }

      // Successful calls are already visible as spans; only slow ones deserve a log line.
      if (durationMs > SLOW_CALL_MS) {
        log.warn(`Slow API call: ${method} ${route} took ${durationMs} ms`, {
          ...attributes,
          'http.response.status_code': response.status,
          'http.client.duration_ms': durationMs,
        })
      }
      return response
    } finally {
      span.end()
    }
  }

  const json = async <T,>(response: Response): Promise<T> =>
    response.status === 204 ? (undefined as T) : ((await response.json()) as T)

  return {
    get: async <T,>(path: string, query?: QueryParams) => json<T>(await send('GET', path, query)),
    post: async <T,>(path: string, body?: unknown) => json<T>(await send('POST', path, undefined, body ?? {})),
    put: async <T,>(path: string, body: unknown) => json<T>(await send('PUT', path, undefined, body)),
    delete: async (path: string) => {
      await send('DELETE', path)
    },
    download: async (path, query, fallbackName) => {
      const response = await send('GET', path, query)
      const disposition = response.headers.get('Content-Disposition') ?? ''
      const fileName = decodeURIComponent(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition)?.[1] ?? fallbackName)

      const objectUrl = URL.createObjectURL(await response.blob())
      const link = Object.assign(document.createElement('a'), { href: objectUrl, download: fileName })
      document.body.append(link)
      link.click()
      link.remove()
      setTimeout(() => URL.revokeObjectURL(objectUrl), 1000)
      return fileName
    },
  }
}
