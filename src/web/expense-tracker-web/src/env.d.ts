/// <reference types="vite/client" />

/** Variables from configs/.env* that are exposed to browser code. */
interface ImportMetaEnv {
  /** Base URL for API calls: `/api` (proxied) or an absolute URL. */
  readonly VITE_API_BASE_URL: string
  /** Address other devices use to reach the app (the login QR code); defaults to the current origin. */
  readonly VITE_PUBLIC_URL?: string
  /** Short label shown next to the logo (e.g. "Dev", "Staging"); unset in production. */
  readonly VITE_ENV_LABEL?: string

  /** OTLP/HTTP endpoint for browser telemetry. Empty = telemetry off. Set by Aspire via OTEL_*. */
  readonly VITE_OTEL_EXPORTER_OTLP_ENDPOINT?: string
  /** Comma-separated key=value headers for the exporter (e.g. x-otlp-api-key). */
  readonly VITE_OTEL_EXPORTER_OTLP_HEADERS?: string
  readonly VITE_OTEL_SERVICE_NAME?: string
  /** Comma-separated key=value resource attributes. */
  readonly VITE_OTEL_RESOURCE_ATTRIBUTES?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
