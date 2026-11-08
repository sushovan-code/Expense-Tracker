/**
 * Browser OpenTelemetry SDK: traces + logs over OTLP/HTTP.
 *
 * - Fetch instrumentation adds a W3C `traceparent` header to API calls, so the API's request span,
 *   its SQL spans and its logs join the browser's trace: click → fetch → API → database.
 * - User-interaction instrumentation creates a span per click/submit and parents the fetches it
 *   triggers (needs the Zone context manager to follow async work).
 * - Log records from `logger` carry the active trace/span ids automatically.
 *
 * Loaded lazily from telemetry/index.ts only when an endpoint is configured.
 */
import { logs } from '@opentelemetry/api-logs'
import { ZoneContextManager } from '@opentelemetry/context-zone'
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-proto'
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-proto'
import { registerInstrumentations } from '@opentelemetry/instrumentation'
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load'
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch'
import { UserInteractionInstrumentation } from '@opentelemetry/instrumentation-user-interaction'
import { resourceFromAttributes } from '@opentelemetry/resources'
import { BatchLogRecordProcessor, LoggerProvider } from '@opentelemetry/sdk-logs'
import { BatchSpanProcessor, WebTracerProvider } from '@opentelemetry/sdk-trace-web'
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions'
import { API_BASE_URL } from '@/shared/config/env'
import { CLIENT_ID } from '@/shared/lib/clientId'
import { otelConfig } from '@/shared/telemetry/config'

const escapeRegExp = (value: string) => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')

export function startOpenTelemetry() {
  const { endpoint, headers } = otelConfig

  const resource = resourceFromAttributes({
    ...otelConfig.resourceAttributes,
    [ATTR_SERVICE_NAME]: otelConfig.serviceName,
    'deployment.environment.name': import.meta.env.MODE,
    // Per-tab id; the API logs the same value (X-Client-Id), so browser and server logs can be joined.
    'session.id': CLIENT_ID,
  })

  // Short delay so telemetry shows up in the dashboard almost immediately during development.
  const batching = { scheduledDelayMillis: import.meta.env.DEV ? 1000 : 5000 }

  const tracerProvider = new WebTracerProvider({
    resource,
    spanProcessors: [new BatchSpanProcessor(new OTLPTraceExporter({ url: `${endpoint}/v1/traces`, headers }), batching)],
  })
  tracerProvider.register({ contextManager: new ZoneContextManager() })

  const loggerProvider = new LoggerProvider({
    resource,
    processors: [
      new BatchLogRecordProcessor({ exporter: new OTLPLogExporter({ url: `${endpoint}/v1/logs`, headers }), ...batching }),
    ],
  })
  logs.setGlobalLoggerProvider(loggerProvider)

  // Relative API URLs are same-origin and get trace headers automatically; an absolute API URL
  // (separately hosted API) has to be allowed explicitly.
  const apiOrigin = /^https?:\/\//.test(API_BASE_URL) ? new URL(API_BASE_URL).origin : null

  registerInstrumentations({
    tracerProvider,
    instrumentations: [
      new DocumentLoadInstrumentation(),
      new UserInteractionInstrumentation({ eventNames: ['click', 'submit'] }),
      new FetchInstrumentation({
        // Never trace the exporter's own requests.
        ignoreUrls: [new RegExp(`^${escapeRegExp(endpoint)}`)],
        propagateTraceHeaderCorsUrls: apiOrigin ? [new RegExp(`^${escapeRegExp(apiOrigin)}`)] : [],
        clearTimingResources: true,
      }),
    ],
  })

  // Flush when the tab is hidden or closed so the last events aren't lost.
  addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') {
      void tracerProvider.forceFlush()
      void loggerProvider.forceFlush()
    }
  })
}
