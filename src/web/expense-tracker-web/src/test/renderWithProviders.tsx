import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, renderHook } from '@testing-library/react'
import type { ReactElement, ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { createFakeSession } from '@/shared/auth/fakeSession'
import type { SessionUser } from '@/shared/auth/session'
import { AuthSessionToken } from '@/shared/auth/useSession'
import { provide, type ServiceRegistration } from '@/shared/di/services'
import { ServicesProvider } from '@/shared/di/ServicesProvider'
import { ToastProvider } from '@/shared/toast/ToastProvider'

interface Options {
  /** Fake implementations, e.g. `[provide(ExpensesApiToken, fakeApi)]`. */
  services?: ServiceRegistration[]
  route?: string
  /** Signed-in user; defaults to one holding every permission. `null` for an anonymous visitor. */
  user?: SessionUser | null
}

/** The app's providers, with injectable services and no network, retries or caching between tests. */
function createWrapper({ services = [], route = '/', user }: Options) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: Infinity }, mutations: { retry: false } } })
  // A fake session first, so a test's own registration can still override it.
  const registrations = [provide(AuthSessionToken, createFakeSession(user)), ...services]
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <ToastProvider>
        <QueryClientProvider client={queryClient}>
          <ServicesProvider services={registrations}>
            <MemoryRouter initialEntries={[route]}>{children}</MemoryRouter>
          </ServicesProvider>
        </QueryClientProvider>
      </ToastProvider>
    )
  }
}

export const renderWithProviders = (ui: ReactElement, options: Options = {}) => render(ui, { wrapper: createWrapper(options) })

/** Renders a hook (e.g. a ViewModel) inside the app's providers. */
export const renderHookWithProviders = <T,>(hook: () => T, options: Options = {}) =>
  renderHook(hook, { wrapper: createWrapper(options) })
