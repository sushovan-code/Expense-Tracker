export type FamilyRole = 'owner' | 'member' | 'viewer'

export const FAMILY_ROLES: FamilyRole[] = ['owner', 'member', 'viewer']

export const FAMILY_ROLE_LABEL: Record<FamilyRole, string> = { owner: 'Owner', member: 'Member', viewer: 'Viewer' }

/** One of my families and my role in it (for the family switcher). */
export interface FamilySummary {
  id: string
  name: string
  role: FamilyRole
}

export interface FamilyMember {
  userId: string
  displayName: string
  email: string
  role: FamilyRole
  joinedAt: string
}

export interface FamilyInvite {
  id: string
  role: FamilyRole
  createdAt: string
  expiresAt: string
}

export interface Family {
  id: string
  name: string
  /** The signed-in user's own role. */
  myRole: FamilyRole
  members: FamilyMember[]
  /** Open invites; only filled in for owners. */
  invites: FamilyInvite[]
}

/** The invite secret is shown once, when it is created. */
export interface CreatedInvite {
  id: string
  token: string
  role: FamilyRole
  expiresAt: string
}

export interface AddMemberInput {
  email: string
  displayName: string
  password: string
  role: FamilyRole
}

export interface AcceptInviteInput {
  token: string
  email: string
  displayName: string
  password: string
}
