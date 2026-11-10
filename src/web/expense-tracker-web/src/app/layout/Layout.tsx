import { Suspense, useEffect, useRef } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { UserMenu } from '@/features/auth'
import { FamilySwitcher } from '@/features/families'
import { Permission, Role } from '@/shared/auth/permissions'
import { useAccess, type Access } from '@/shared/auth/useSession'
import { ENV_LABEL } from '@/shared/config/env'
import { ConnectionIndicator } from '@/shared/realtime/ConnectionIndicator'
import { RealtimeNotifications } from '../realtime/RealtimeNotifications'
import { RealtimeQuerySync } from '../realtime/RealtimeQuerySync'
import { logger } from '@/shared/telemetry'
import { Backdrop } from './Backdrop'
import { WalletIcon } from '@/shared/ui/Icons'
import { LoadingState } from '@/shared/ui/States'
import { ThemeToggle } from '@/shared/theme/ThemeToggle'

const NAV: { to: string; label: string; end: boolean; visible: (access: Access) => boolean }[] = [
  { to: '/', label: 'Dashboard', end: true, visible: (a) => a.can(Permission.ExpenseView) },
  { to: '/expenses', label: 'Expenses', end: false, visible: (a) => a.can(Permission.ExpenseView) },
  { to: '/categories', label: 'Categories', end: false, visible: (a) => a.can(Permission.CategoryView) },
  { to: '/family', label: 'Family', end: false, visible: () => true },
  {
    to: '/admin',
    label: 'Admin',
    end: false,
    visible: (a) => a.isAtLeast(Role.Admin) && (a.can(Permission.UserView) || a.can(Permission.RoleView)),
  },
]

export function Layout() {
  const { pathname } = useLocation()
  const access = useAccess()

  // The ref guard skips StrictMode's development-only double effect run.
  const lastLoggedPath = useRef<string | null>(null)
  useEffect(() => {
    if (lastLoggedPath.current === pathname) return
    lastLoggedPath.current = pathname
    logger.event('app.navigated', `Navigated to ${pathname}`, { 'app.route': pathname })
  }, [pathname])

  return (
    <div className="app">
      <Backdrop />
      <RealtimeNotifications />
      <RealtimeQuerySync />

      <header className="app-header">
        <div className="app-header__inner">
          <NavLink to="/" className="brand">
            <span className="brand__mark">
              <WalletIcon size={18} />
            </span>
            <span className="brand__name">Expense Tracker</span>
            {ENV_LABEL && (
              <span className="env-badge" title={`Running against the ${ENV_LABEL} environment`}>
                {ENV_LABEL}
              </span>
            )}
          </NavLink>

          <nav className="nav" aria-label="Main">
            {NAV.filter((item) => item.visible(access)).map((item) => (
              <NavLink key={item.to} to={item.to} end={item.end} className="nav__link">
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="app-header__end">
            <FamilySwitcher />
            <ConnectionIndicator />
            <ThemeToggle />
            <UserMenu />
          </div>
        </div>
      </header>

      {/* Keyed by route so each page plays its entrance animation. */}
      <main key={pathname} className="app-main page">
        <Suspense fallback={<LoadingState />}>
          <Outlet />
        </Suspense>
      </main>
    </div>
  )
}
