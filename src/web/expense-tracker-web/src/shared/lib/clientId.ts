/**
 * Identifies this browser tab across reloads (sessionStorage is per tab).
 *
 * Sent to the API as X-Client-Id and to the SignalR hub, so the server can tag logs with it and
 * this tab can recognise (and skip) realtime echoes of its own changes. It is random and carries
 * no personal data.
 */
const STORAGE_KEY = 'expense-tracker.client-id'

function createId(): string {
  return typeof crypto.randomUUID === 'function'
    ? crypto.randomUUID()
    : `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`
}

function resolveClientId(): string {
  try {
    const existing = sessionStorage.getItem(STORAGE_KEY)
    if (existing) return existing
    const id = createId()
    sessionStorage.setItem(STORAGE_KEY, id)
    return id
  } catch {
    return createId()
  }
}

export const CLIENT_ID = resolveClientId()
