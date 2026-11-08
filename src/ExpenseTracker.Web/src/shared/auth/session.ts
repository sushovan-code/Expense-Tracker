import type { HttpAuth } from '@/shared/api/httpClient'
import { toApiError, unreachable } from '@/shared/api/apiError'
import { logger } from '@/shared/telemetry'

/** The signed-in user, as returned by the API (CurrentUserDto). */
export interface SessionUser {
  id: string
  email: string
  displayName: string
  roles: string[]
  permissions: string[]
}

export type SessionStatus = 'restoring' | 'authenticated' | 'anonymous'

export interface SessionState {
  status: SessionStatus
  user: SessionUser | null
  /** Why the last session ended; lets the login page explain an unexpected sign-out. */
  endedBecause?: 'expired' | 'signed-out'
}

export interface AuthSession extends HttpAuth {
  getState(): SessionState
  subscribe(listener: () => void): () => void
  /** On startup: resumes the session from the refresh cookie, if there is one. */
  restore(): Promise<void>
  /** Throws ApiError (401 with the server's message, 429 when rate limited, ...). */
  signIn(email: string, password: string): Promise<SessionUser>
  signOut(): Promise<void>
}

interface AuthResponse {
  accessToken: string
  expiresIn: number
  user: SessionUser
}

export interface AuthSessionOptions {
  baseUrl: string
  clientId: string
  fetch?: typeof fetch
}

const CHANNEL = 'expense-tracker.auth'
const REFRESH_LOCK = 'expense-tracker.auth.refresh'
const RETRY_AFTER_ERROR_MS = 15_000

/**
 * Browser session for a SPA, following the OAuth-for-browser-apps guidance:
 * - The access token lives only in memory (never localStorage), so injected scripts can't lift it from storage.
 * - The refresh token is an HttpOnly, SameSite=Strict cookie set by the API; page scripts never see it.
 * - The access token is renewed a minute before it expires, and on demand when a call gets a 401.
 * - Refreshes are single-flight per tab and serialised across tabs (Web Locks), so two tabs never present
 *   the same one-time refresh token (which the server would treat as theft and revoke the session).
 * - Sign-in and sign-out are broadcast to other tabs.
 */
export function createAuthSession({ baseUrl, clientId, fetch: fetchImpl = globalThis.fetch.bind(globalThis) }: AuthSessionOptions): AuthSession {
  let state: SessionState = { status: 'restoring', user: null }
  let accessToken: string | null = null
  let refreshTimer: ReturnType<typeof setTimeout> | undefined
  let inflight: Promise<boolean> | null = null
  const listeners = new Set<() => void>()
  const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(CHANNEL)

  function setState(next: SessionState) {
    state = next
    listeners.forEach((listener) => listener())
  }

  const post = (path: string, body?: unknown) =>
    fetchImpl(`${baseUrl}${path}`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        Accept: 'application/json',
        'X-Client-Id': clientId,
        ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    })

  function scheduleRefresh(delayMs: number) {
    clearTimeout(refreshTimer)
    refreshTimer = setTimeout(() => void refresh(), Math.max(5_000, delayMs))
  }

  function apply(response: AuthResponse) {
    accessToken = response.accessToken
    // A minute early, or at 80% of an unusually short lifetime.
    const lifetimeMs = response.expiresIn * 1000
    scheduleRefresh(Math.min(lifetimeMs - 60_000, lifetimeMs * 0.8))

    // Keep the snapshot stable when nothing changed, so subscribers don't re-render on every renewal.
    if (state.status !== 'authenticated' || JSON.stringify(state.user) !== JSON.stringify(response.user)) {
      setState({ status: 'authenticated', user: response.user })
    }
  }

  function end(reason: 'expired' | 'signed-out') {
    accessToken = null
    clearTimeout(refreshTimer)
    if (state.status !== 'anonymous') setState({ status: 'anonymous', user: null, endedBecause: reason })
  }

  async function exchangeRefreshToken(): Promise<'ok' | 'unauthorized' | 'error'> {
    let response: Response
    try {
      response = await post('/auth/refresh')
    } catch {
      return 'error'
    }
    if (response.ok) {
      apply((await response.json()) as AuthResponse)
      return 'ok'
    }
    return response.status === 401 ? 'unauthorized' : 'error'
  }

  const withCrossTabLock = <T,>(task: () => Promise<T>): Promise<T> =>
    typeof navigator !== 'undefined' && navigator.locks
      ? (navigator.locks.request(REFRESH_LOCK, task) as Promise<T>)
      : task()

  function refresh(): Promise<boolean> {
    inflight ??= withCrossTabLock(exchangeRefreshToken)
      .then((outcome) => {
        if (outcome === 'unauthorized') {
          if (state.status === 'authenticated') logger.event('auth.session_expired', 'Session expired or was revoked', {}, 'warn')
          end('expired')
        } else if (outcome === 'error' && state.status === 'authenticated') {
          // Server unreachable: keep the session and try again shortly.
          scheduleRefresh(RETRY_AFTER_ERROR_MS)
        }
        return outcome === 'ok'
      })
      .finally(() => {
        inflight = null
      })
    return inflight
  }

  channel?.addEventListener('message', (event: MessageEvent<string>) => {
    if (event.data === 'signed-out') end('signed-out')
    // The shared refresh cookie now belongs to whoever signed in there; follow it.
    else if (event.data === 'signed-in') void refresh()
  })

  return {
    getState: () => state,
    getAccessToken: () => accessToken,
    refresh,

    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },

    async restore() {
      const resumed = await refresh()
      if (!resumed && state.status === 'restoring') setState({ status: 'anonymous', user: null })
      if (resumed) logger.event('auth.session_restored', 'Session restored', { 'enduser.id': state.user?.id })
    },

    async signIn(email, password) {
      let response: Response
      try {
        response = await post('/auth/login', { email, password })
      } catch {
        throw unreachable()
      }
      if (!response.ok) {
        const { error } = await toApiError(response)
        logger.event('auth.sign_in_failed', `Sign-in failed (${response.status})`, { 'http.response.status_code': response.status }, 'warn')
        throw error
      }

      const body = (await response.json()) as AuthResponse
      apply(body)
      channel?.postMessage('signed-in')
      logger.event('auth.signed_in', 'Signed in', { 'enduser.id': body.user.id, 'enduser.roles': body.user.roles.join(',') })
      return body.user
    },

    async signOut() {
      const userId = state.user?.id
      try {
        await post('/auth/logout')
      } catch {
        // Best effort: the local session ends regardless, and the server-side one expires on its own.
      }
      end('signed-out')
      channel?.postMessage('signed-out')
      logger.event('auth.signed_out', 'Signed out', { 'enduser.id': userId })
    },
  }
}
