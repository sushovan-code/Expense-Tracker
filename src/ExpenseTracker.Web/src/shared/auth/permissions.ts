/**
 * Mirrors the server's permission catalog (ExpenseTracker.Application/Security/Permissions.cs).
 * The UI uses these only to decide what to show; the API enforces them on every request.
 */
export const Permission = {
  ExpenseView: 'expense.view',
  ExpenseAdd: 'expense.add',
  ExpenseEdit: 'expense.edit',
  ExpenseDelete: 'expense.delete',
  ExpenseExport: 'expense.export',
  CategoryView: 'category.view',
  CategoryAdd: 'category.add',
  CategoryEdit: 'category.edit',
  CategoryDelete: 'category.delete',
  BudgetView: 'budget.view',
  UserView: 'user.view',
  UserManage: 'user.manage',
  RoleView: 'role.view',
  RoleManage: 'role.manage',
  ApiKeyManage: 'apikey.manage',
} as const

export type Permission = (typeof Permission)[keyof typeof Permission]

/** Built-in roles, highest first (mirrors AppRoles.cs). */
export const Role = {
  SuperAdmin: 'SuperAdmin',
  Admin: 'Admin',
  User: 'User',
  Reader: 'Reader',
} as const

export type Role = (typeof Role)[keyof typeof Role]

const RANK: Record<string, number> = { SuperAdmin: 400, Admin: 300, User: 200, Reader: 100 }

export const roleRank = (role: string) => RANK[role] ?? 0

export const highestRank = (roles: readonly string[]) => Math.max(0, ...roles.map(roleRank))
