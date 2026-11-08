import { createHttpClient } from '@/shared/api/httpClient'
import { createAuthSession } from './session'

const user = { id: 'u1', email: 'reader@example.com', displayName: 'Reader', roles: ['Reader'], permissions: ['expense.view'] }

const json = (status: number, body?: unknown) =>
  new Response(body === undefined ? null : JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

const tokenResponse = (token: string) => json(200, { accessToken: token, expiresIn: 900, user })

/** A fake API: answers by "METHOD path", recording every call. */
function fakeFetch(routes: Record<string, (init: RequestInit) => Response | Promise<Response>>) {
  const calls: { key: string; init: RequestInit }[] = []
  const fetch = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const key = `${init.method ?? 'GET'} ${String(input).replace(/\?.*$/, '')}`
    calls.push({ key, init })
    const route = routes[key]
    if (!route) throw new Error(`Unexpected call ${key}`)
    return route(init)
  })
  return { fetch: fetch as unknown as typeof globalThis.fetch, calls }
}

describe('auth session', () => {
  beforeEach(() => vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] }))
  afterEach(() => vi.useRealTimers())

  it('restores a session from the refresh cookie, or becomes anonymous', async () => {
    const ok = fakeFetch({ 'POST /api/auth/refresh': () => tokenResponse('t1') })
    const restored = createAuthSession({ baseUrl: '/api', clientId: 'c', fetch: ok.fetch })
    await restored.restore()
    expect(restored.getState()).toMatchObject({ status: 'authenticated', user })
    expect(restored.getAccessToken()).toBe('t1')
    expect(ok.calls[0].init.credentials).toBe('include')

    const none = fakeFetch({ 'POST /api/auth/refresh': () => json(401) })
    const anonymous = createAuthSession({ baseUrl: '/api', clientId: 'c', fetch: none.fetch })
    await anonymous.restore()
    expect(anonymous.getState().status).toBe('anonymous')
    expect(anonymous.getAccessToken()).toBeNull()
  })

  it('signs in, keeps the token in memory only, and surfaces the server message on failure', async () => {
    const api = fakeFetch({
      'POST /api/auth/login': (init) =>
        JSON.parse(String(init.body)).password === 'right'
          ? tokenResponse('t1')
          : json(401, { title: 'Sign-in failed', detail: 'Invalid email or password.' }),
    })
    const session = createAuthSession({ baseUrl: '/api', clientId: 'c', fetch: api.fetch })

    await expect(session.signIn('reader@example.com', 'wrong')).rejects.toMatchObject({ status: 401, message: 'Invalid email or password.' })
    expect(session.getState().status).toBe('restoring')

    await session.signIn('reader@example.com', 'right')
    expect(session.getAccessToken()).toBe('t1')
    const stored = [localStorage, sessionStorage].flatMap((storage) =>
      Array.from({ length: storage.length }, (_, i) => storage.getItem(storage.key(i)!) ?? ''))
    expect(stored.some((value) => value.includes('t1'))).toBe(false)
  })

  it('refreshes once for concurrent callers', async () => {
    let release: (r: Response) => void = () => {}
    const api = fakeFetch({ 'POST /api/auth/refresh': () => new Promise<Response>((resolve) => (release = resolve)) })
    const session = createAuthSession({ baseUrl: '/api', clientId: 'c', fetch: api.fetch })

    const both = Promise.all([session.refresh(), session.refresh()])
    await vi.waitFor(() => expect(api.calls).toHaveLength(1))
    release(tokenResponse('t2'))

    expect(await both).toEqual([true, true])
    expect(api.calls).toHaveLength(1)
  })

  it('ends the session when the refresh token is rejected', async () => {
    let valid = true
    const api = fakeFetch({ 'POST /api/auth/refresh': () => (valid ? tokenResponse('t1') : json(401)) })
    const session = createAuthSession({ baseUrl: '/api', clientId: 'c', fetch: api.fetch })
    await session.restore()

    valid = false
    expect(await session.refresh()).toBe(false)
    expect(session.getState()).toMatchObject({ status: 'anonymous', endedBecause: 'expired' })
  })

  it('signs out on the server and locally even if the server is unreachable', async () => {
    const api = fakeFetch({
      'POST /api/auth/refresh': () => tokenResponse('t1'),
      'POST /api/auth/logout': () => Promise.reject(new TypeError('Failed to fetch')),
    })
    const session = createAuthSession({ baseUrl: '/api', clientId: 'c', fetch: api.fetch })
    await session.restore()

    await session.signOut()

    expect(api.calls.map((c) => c.key)).toContain('POST /api/auth/logout')
    expect(session.getState()).toMatchObject({ status: 'anonymous', endedBecause: 'signed-out' })
    expect(session.getAccessToken()).toBeNull()
  })
})

describe('http client with a session', () => {
  it('sends the bearer token, and on a 401 refreshes once and retries', async () => {
    let currentToken = 'old'
    const seen: (string | null)[] = []
    const fetch = vi.fn(async (_input: RequestInfo | URL, init: RequestInit = {}) => {
      const auth = new Headers(init.headers).get('Authorization')
      seen.push(auth)
      return auth === 'Bearer new' ? json(200, { ok: true }) : json(401)
    }) as unknown as typeof globalThis.fetch
    const refresh = vi.fn(async () => {
      currentToken = 'new'
      return true
    })
    const http = createHttpClient({ baseUrl: '/api', clientId: 'c', fetch, auth: { getAccessToken: () => currentToken, refresh } })

    await expect(http.get('/expenses')).resolves.toEqual({ ok: true })
    expect(seen).toEqual(['Bearer old', 'Bearer new'])
    expect(refresh).toHaveBeenCalledTimes(1)
  })

  it('gives up after one retry', async () => {
    const fetch = vi.fn(async () => json(401)) as unknown as typeof globalThis.fetch
    const refresh = vi.fn(async () => true)
    const http = createHttpClient({ baseUrl: '/api', clientId: 'c', fetch, auth: { getAccessToken: () => 't', refresh } })

    await expect(http.get('/expenses')).rejects.toMatchObject({ status: 401 })
    expect(refresh).toHaveBeenCalledTimes(1)
    expect(fetch).toHaveBeenCalledTimes(2)
  })
})
