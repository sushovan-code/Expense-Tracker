import { fileURLToPath, URL } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

/**
 * Environment files live in ./configs (.env, .env.<mode>, .env.<mode>.local).
 * Each mode pairs with the API environment of the same name:
 *   development ↔ Development, staging ↔ Staging, production ↔ Production.
 * Real environment variables (e.g. injected by the Aspire AppHost) take precedence over the files.
 */
const ENV_DIR = 'configs'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  // '' prefix loads non-VITE_ variables too; they're only used here, never shipped to the browser.
  const env = loadEnv(mode, ENV_DIR, '')

  const port = Number(env.CLIENT_PORT)
  const apiTarget = env.API_PROXY_TARGET
  if (!port || !apiTarget) {
    throw new Error(
      `Missing CLIENT_PORT or API_PROXY_TARGET for mode "${mode}". Check ${ENV_DIR}/.env.${mode}.`,
    )
  }

  // Browser telemetry: hand the standard OTEL_* variables (set by Aspire) to the client bundle.
  // VITE_OTEL_* in configs/.env.<mode> can be used instead when running without Aspire.
  const otel = {
    VITE_OTEL_EXPORTER_OTLP_ENDPOINT: env.VITE_OTEL_EXPORTER_OTLP_ENDPOINT || env.OTEL_EXPORTER_OTLP_ENDPOINT || '',
    VITE_OTEL_EXPORTER_OTLP_HEADERS: env.VITE_OTEL_EXPORTER_OTLP_HEADERS || env.OTEL_EXPORTER_OTLP_HEADERS || '',
    VITE_OTEL_SERVICE_NAME: env.VITE_OTEL_SERVICE_NAME || env.OTEL_SERVICE_NAME || '',
    VITE_OTEL_RESOURCE_ATTRIBUTES: env.VITE_OTEL_RESOURCE_ATTRIBUTES || env.OTEL_RESOURCE_ATTRIBUTES || '',
  }

  const proxy = {
    '/api': { target: apiTarget, changeOrigin: true },
    // SignalR (negotiate over HTTP, then WebSockets).
    '/hubs': { target: apiTarget, changeOrigin: true, ws: true },
  }

  return {
    plugins: [react()],
    resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
    envDir: ENV_DIR,
    define: Object.fromEntries(
      Object.entries(otel).map(([key, value]) => [`import.meta.env.${key}`, JSON.stringify(value)]),
    ),
    // strictPort: fail instead of silently moving to another port the API's CORS list doesn't know.
    server: { port, strictPort: true, proxy },
    preview: { port, strictPort: true, proxy },
    build: { outDir: mode === 'production' ? 'dist' : `dist-${mode}` },
  }
})
