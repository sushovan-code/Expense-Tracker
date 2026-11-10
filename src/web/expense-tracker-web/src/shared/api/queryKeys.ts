/**
 * Root keys of the TanStack Query cache, one per kind of server data.
 *
 * Features build their keys under these roots; invalidating a root refreshes everything beneath it.
 * Kept in shared so a change in one feature (e.g. an expense) can refresh data owned by another
 * (budgets, category counts) without the features importing each other.
 */
export const QueryRoots = {
  expenses: 'expenses',
  categories: 'categories',
  budgets: 'budgets',
  /** The signed-in user's family: members, roles and invites. */
  family: 'family',
  /** The signed-in user's own account data (API keys). */
  account: 'account',
  /** User and role administration. */
  admin: 'admin',
} as const

export type QueryRoot = (typeof QueryRoots)[keyof typeof QueryRoots]
