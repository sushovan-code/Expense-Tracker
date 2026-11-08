import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from '@/app/App'
import { AppProviders } from '@/app/providers/AppProviders'
import { getAuthSession } from '@/app/services'
import { startRealtime, stopRealtime } from '@/shared/realtime/connection'
import { initTelemetry } from '@/shared/telemetry'
import '@/app/styles/tokens.css'
import '@/app/styles/global.css'
import '@/app/styles/components.css'
import '@/app/styles/auth.css'

// Bootstrap: telemetry first (so the page load and early errors are captured), then the session
// (resumed from the refresh cookie), then render. Realtime follows the session: the hub requires sign-in.
void initTelemetry().finally(() => {
  const session = getAuthSession()
  session.subscribe(() => {
    if (session.getState().status === 'authenticated') void startRealtime(session.getAccessToken)
    else void stopRealtime()
  })
  void session.restore()

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <AppProviders>
        <App />
      </AppProviders>
    </StrictMode>,
  )
})
