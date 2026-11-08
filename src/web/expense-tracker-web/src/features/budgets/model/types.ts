export type BudgetState = 'ok' | 'warning' | 'exceeded'

export interface BudgetStatus {
  categoryId: string
  categoryName: string
  categoryColor: string
  budget: number
  spent: number
  remaining: number
  percentUsed: number
  state: BudgetState
}

export interface BudgetOverview {
  year: number
  month: number
  totalBudget: number
  totalSpent: number
  categories: BudgetStatus[]
}
