/**
 * OpenTelemetry exporter settings for the browser.
 *
 * Under the Aspire AppHost these come from the standard OTEL_* variables Aspire injects into the
 * Vite process (mapped to VITE_OTEL_* in vite.config.ts). Outside Aspire they can be set in
 * configs/.env.<mode> to point at any OTLP/HTTP collector. With no endpoint, telemetry is off and
 * the logger only writes to the console.
 */
const env = import.meta.env

function parseKeyValues(raw: string | undefined): Record<string, string> {
  const result: Record<string, string> = {}
  for (const pair of (raw ?? '').split(',')) {
    const index = pair.indexOf('=')
    if (index <= 0) continue
    result[decodeURIComponent(pair.slice(0, index).trim())] = decodeURIComponent(pair.slice(index + 1).trim())
  }
  return result
}

export const otelConfig = {
  /** OTLP/HTTP base URL, e.g. https://localhost:19011 (signal paths are appended). */
  endpoint: (env.VITE_OTEL_EXPORTER_OTLP_ENDPOINT ?? '').replace(/\/+$/, ''),
  /** Extra headers, e.g. the Aspire dashboard's x-otlp-api-key. */
  headers: parseKeyValues(env.VITE_OTEL_EXPORTER_OTLP_HEADERS),
  serviceName: env.VITE_OTEL_SERVICE_NAME || 'expense-tracker-client',
  resourceAttributes: parseKeyValues(env.VITE_OTEL_RESOURCE_ATTRIBUTES),
}

export const telemetryEnabled = otelConfig.endpoint !== ''
