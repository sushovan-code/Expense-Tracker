import { QueryClientProvider, type QueryClient } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { ServicesProvider } from '@/shared/di/ServicesProvider'
import type { ServiceRegistration } from '@/shared/di/services'
import { ThemeProvider } from '@/shared/theme/ThemeProvider'
import { ToastProvider } from '@/shared/toast/ToastProvider'
import { ErrorBoundary } from '@/shared/ui/ErrorBoundary'
import { createQueryClient } from '../queryClient'
import { createAppServices } from '../services'
import { SessionCacheGuard } from './SessionCacheGuard'

interface AppProvidersProps {
  children: ReactNode
  /** Overridable for tests and storybook-style hosts. */
  services?: ServiceRegistration[]
  queryClient?: QueryClient
}

/** Every cross-cutting provider, in one place and in dependency order. */
export function AppProviders({ children, services, queryClient }: AppProvidersProps) {
  // Created once per app instance (not per render).
  const [registrations] = useState(() => services ?? createAppServices())
  const [client] = useState(() => queryClient ?? createQueryClient())

  return (
    <ErrorBoundary>
      <ThemeProvider>
        <ToastProvider>
          <QueryClientProvider client={client}>
            <ServicesProvider services={registrations}>
              <SessionCacheGuard />
              <BrowserRouter>{children}</BrowserRouter>
            </ServicesProvider>
          </QueryClientProvider>
        </ToastProvider>
      </ThemeProvider>
    </ErrorBoundary>
  )
}
