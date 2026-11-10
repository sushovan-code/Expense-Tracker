import { AdminApiToken, createAdminApi } from '@/features/admin'
import { AccountApiToken, createAccountApi } from '@/features/auth'
import { BudgetsApiToken, createBudgetsApi } from '@/features/budgets'
import { CategoriesApiToken, createCategoriesApi } from '@/features/categories'
import { createExpensesApi, ExpensesApiToken } from '@/features/expenses'
import { createFamiliesApi, FamiliesApiToken } from '@/features/families'
import { createHttpClient, HttpClientToken } from '@/shared/api/httpClient'
import { createAuthSession, type AuthSession } from '@/shared/auth/session'
import { AuthSessionToken } from '@/shared/auth/useSession'
import { API_BASE_URL } from '@/shared/config/env'
import { provide, type ServiceRegistration } from '@/shared/di/services'
import { CLIENT_ID } from '@/shared/lib/clientId'

let session: AuthSession | undefined

/** The browser session: one per page, shared by the HTTP client, realtime and the UI. */
export const getAuthSession = () => (session ??= createAuthSession({ baseUrl: API_BASE_URL, clientId: CLIENT_ID }))

/**
 * Composition root: the one place that decides which implementation backs each service token.
 * Swap an implementation here (or in a test's ServicesProvider) without touching any feature.
 */
export function createAppServices(): ServiceRegistration[] {
  const auth = getAuthSession()
  const http = createHttpClient({ baseUrl: API_BASE_URL, clientId: CLIENT_ID, auth })

  return [
    provide(AuthSessionToken, auth),
    provide(HttpClientToken, http),
    provide(ExpensesApiToken, createExpensesApi(http)),
    provide(CategoriesApiToken, createCategoriesApi(http)),
    provide(BudgetsApiToken, createBudgetsApi(http)),
    provide(AccountApiToken, createAccountApi(http)),
    provide(AdminApiToken, createAdminApi(http)),
    provide(FamiliesApiToken, createFamiliesApi(http)),
  ]
}
