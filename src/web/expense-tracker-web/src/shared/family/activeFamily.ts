const STORAGE_KEY = 'expense-tracker.activeFamily'

/** The header the API reads to know which of the person's families this request is about. */
export const FAMILY_HEADER = 'X-Family-Id'

/**
 * The family the person is working in. A person can belong to several families; the choice is kept in this
 * browser only and sent with every request. The API ignores an id the person doesn't belong to and falls back
 * to their earliest joined family, so a stale value is harmless.
 */
export function getActiveFamilyId(): string | null {
  try {
    return window.localStorage.getItem(STORAGE_KEY)
  } catch {
    return null // storage can be blocked (private windows); the API then uses the default family
  }
}

export function setActiveFamilyId(id: string): void {
  try {
    window.localStorage.setItem(STORAGE_KEY, id)
  } catch {
    // Nothing to do: the choice just won't be remembered.
  }
}

/**
 * Makes `id` the active family and reloads the app at `path`. A reload is the simplest way to refresh every
 * screen and to rejoin the live-update group of the new family, which is picked when the connection starts.
 */
export function switchActiveFamily(id: string, path: string = `${window.location.pathname}${window.location.search}`): void {
  setActiveFamilyId(id)
  window.location.assign(path)
}
