import { API_BASE_URL } from '@/shared/config/env'
import { CLIENT_ID } from '@/shared/lib/clientId'
import { otelConfig, telemetryEnabled } from '@/shared/telemetry/config'
import { logger } from '@/shared/telemetry/logger'
import { reportWebVitals } from '@/shared/telemetry/webVitals'

export { logger } from '@/shared/telemetry/logger'

let started: Promise<void> | undefined

/**
 * Starts browser telemetry (when an OTLP endpoint is configured) and global error capture.
 * Call once, before rendering, so the first page load and early errors are recorded.
 */
export function initTelemetry(): Promise<void> {
  started ??= (async () => {
    if (telemetryEnabled) {
      try {
        // Lazy-loaded so the SDK (and zone.js) only ship to the browser when telemetry is on.
        const { startOpenTelemetry } = await import('@/shared/telemetry/otel')
        startOpenTelemetry()
      } catch (error) {
        console.error('Failed to start OpenTelemetry; continuing without it.', error)
      }
    }

    addEventListener('error', (event) => {
      logger.error(`Unhandled error: ${event.message}`, event.error ?? event.message, {
        'code.filepath': event.filename,
        'code.lineno': event.lineno,
        'code.column': event.colno,
      })
    })
    addEventListener('unhandledrejection', (event) => {
      const reason = event.reason instanceof Error ? event.reason.message : String(event.reason)
      logger.error(`Unhandled promise rejection: ${reason}`, event.reason)
    })

    addEventListener('online', () => logger.event('browser.online', 'Browser is back online'))
    addEventListener('offline', () => logger.event('browser.offline', 'Browser went offline', undefined, 'warn'))

    reportWebVitals()

    logger.event('app.started', `Client started in ${import.meta.env.MODE} mode`, {
      'deployment.environment.name': import.meta.env.MODE,
      'session.id': CLIENT_ID,
      'app.api_base_url': API_BASE_URL,
      'telemetry.enabled': telemetryEnabled,
      'telemetry.endpoint': otelConfig.endpoint || undefined,
      'user_agent.original': navigator.userAgent,
      'screen.viewport': `${innerWidth}x${innerHeight}`,
      'app.color_scheme': matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light',
    })
  })()
  return started
}
