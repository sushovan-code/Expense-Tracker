import type { HttpClient } from '@/shared/api/httpClient'
import { createServiceToken } from '@/shared/di/services'
import type { AcceptInviteInput, AddMemberInput, CreatedInvite, Family, FamilyRole, FamilySummary } from '../model/types'

/** Family endpoints. View-models depend on this interface (resolved via DI), not on HTTP details. */
export interface FamiliesApi {
  /** Every family I belong to, earliest joined first. */
  listMine(): Promise<FamilySummary[]>
  /** The active family with its members (the one named by the X-Family-Id header, else the earliest joined). */
  getMine(): Promise<Family>
  create(name: string): Promise<Family>
  rename(name: string): Promise<Family>
  addMember(input: AddMemberInput): Promise<Family>
  changeRole(userId: string, role: FamilyRole): Promise<void>
  removeMember(userId: string): Promise<void>
  leave(): Promise<void>
  createInvite(role: FamilyRole): Promise<CreatedInvite>
  revokeInvite(id: string): Promise<void>
  /** A signed-in user without a family joins with an invite token. */
  join(token: string): Promise<Family>
  /** Anonymous: creates the account and joins the family. */
  acceptInvite(input: AcceptInviteInput): Promise<void>
}

export const FamiliesApiToken = createServiceToken<FamiliesApi>('FamiliesApi')

export function createFamiliesApi(http: HttpClient): FamiliesApi {
  const member = (userId: string) => `/families/members/${encodeURIComponent(userId)}`
  return {
    listMine: () => http.get('/families'),
    getMine: () => http.get('/families/mine'),
    create: (name) => http.post('/families', { name }),
    rename: (name) => http.put('/families/mine', { name }),
    addMember: (input) => http.post('/families/members', input),
    changeRole: (userId, role) => http.put(`${member(userId)}/role`, { role }),
    removeMember: (userId) => http.delete(member(userId)),
    leave: () => http.post('/families/leave'),
    createInvite: (role) => http.post('/families/invites', { role }),
    revokeInvite: (id) => http.delete(`/families/invites/${encodeURIComponent(id)}`),
    join: (token) => http.post('/families/invites/join', { token }),
    acceptInvite: (input) => http.post('/families/invites/accept', input),
  }
}
