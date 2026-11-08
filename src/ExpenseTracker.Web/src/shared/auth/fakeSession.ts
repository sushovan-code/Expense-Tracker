import { Permission, Role } from './permissions'
import type { AuthSession, SessionState, SessionUser } from './session'

/** A signed-in user holding every permission, for tests that aren't about access control. */
export const testUser: SessionUser = {
  id: '00000000-0000-0000-0000-000000000001',
  email: 'test@example.com',
  displayName: 'Test User',
  roles: [Role.SuperAdmin],
  permissions: Object.values(Permission),
}

/** In-memory AuthSession for tests and previews: no network, no timers. */
export function createFakeSession(user: SessionUser | null = testUser): AuthSession {
  let state: SessionState = user ? { status: 'authenticated', user } : { status: 'anonymous', user: null }
  const listeners = new Set<() => void>()
  const set = (next: SessionState) => {
    state = next
    listeners.forEach((l) => l())
  }

  return {
    getState: () => state,
    getAccessToken: () => (state.user ? 'fake-token' : null),
    refresh: async () => state.status === 'authenticated',
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    restore: async () => {},
    signIn: async () => {
      const signedIn = user ?? testUser
      set({ status: 'authenticated', user: signedIn })
      return signedIn
    },
    signOut: async () => set({ status: 'anonymous', user: null, endedBecause: 'signed-out' }),
  }
}
