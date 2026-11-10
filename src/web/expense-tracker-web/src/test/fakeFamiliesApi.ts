import type { FamiliesApi, Family } from '@/features/families'

export const sharma: Family = {
  id: 'fam-1',
  name: 'Sharma family',
  myRole: 'owner',
  members: [
    { userId: 'user-1', displayName: 'Asha', email: 'asha@example.com', role: 'owner', joinedAt: '2026-11-01T10:00:00Z' },
    { userId: 'user-2', displayName: 'Ravi', email: 'ravi@example.com', role: 'member', joinedAt: '2026-11-02T10:00:00Z' },
  ],
  invites: [],
}

/** A families API fake whose methods are spies; pass overrides for the cases a test cares about. */
export function fakeFamiliesApi(overrides: Partial<FamiliesApi> = {}): FamiliesApi {
  return {
    getMine: vi.fn().mockResolvedValue(sharma),
    create: vi.fn().mockResolvedValue(sharma),
    rename: vi.fn().mockResolvedValue(sharma),
    addMember: vi.fn().mockResolvedValue(sharma),
    changeRole: vi.fn().mockResolvedValue(undefined),
    removeMember: vi.fn().mockResolvedValue(undefined),
    leave: vi.fn().mockResolvedValue(undefined),
    createInvite: vi.fn().mockResolvedValue({ id: 'inv-1', token: 'secret-token', role: 'member', expiresAt: '2026-11-12T10:00:00Z' }),
    revokeInvite: vi.fn().mockResolvedValue(undefined),
    join: vi.fn().mockResolvedValue(sharma),
    acceptInvite: vi.fn().mockResolvedValue(undefined),
    ...overrides,
  }
}
