import { lazy } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { AccountPage, LoginPage, LogoutPage, RegisterPage, RequireAuth } from '@/features/auth'
import { AcceptInvitePage } from '@/features/families'
import { Permission, Role } from '@/shared/auth/permissions'
import { Layout } from './layout/Layout'

// Route-level code splitting per feature: e.g. the charting library only loads with the dashboard.
const DashboardPage = lazy(() => import('@/features/dashboard').then((m) => ({ default: m.DashboardPage })))
const ExpensesPage = lazy(() => import('@/features/expenses').then((m) => ({ default: m.ExpensesPage })))
const CategoriesPage = lazy(() => import('@/features/categories').then((m) => ({ default: m.CategoriesPage })))
const FamilyPage = lazy(() => import('@/features/families').then((m) => ({ default: m.FamilyPage })))
const AdminPage = lazy(() => import('@/features/admin').then((m) => ({ default: m.AdminPage })))

/**
 * Routes. Each screen is a feature's page (View) backed by its ViewModel hook.
 * Everything except sign-in/out needs a session; each page also declares the permission it needs.
 * (The API enforces the same permissions; the guards just avoid showing pages that would only fail.)
 */
export function App() {
  return (
    <Routes>
      <Route path="login" element={<LoginPage />} />
      <Route path="logout" element={<LogoutPage />} />
      <Route path="register" element={<RegisterPage />} />
      <Route path="accept-invite" element={<AcceptInvitePage />} />

      <Route element={<RequireAuth />}>
        <Route element={<Layout />}>
          <Route index element={<RequireAuth permission={Permission.ExpenseView}><DashboardPage /></RequireAuth>} />
          <Route path="expenses" element={<RequireAuth permission={Permission.ExpenseView}><ExpensesPage /></RequireAuth>} />
          <Route path="categories" element={<RequireAuth permission={Permission.CategoryView}><CategoriesPage /></RequireAuth>} />
          <Route path="family" element={<FamilyPage />} />
          <Route path="account" element={<AccountPage />} />
          <Route path="admin" element={<RequireAuth minRole={Role.Admin}><AdminPage /></RequireAuth>} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Route>
    </Routes>
  )
}
