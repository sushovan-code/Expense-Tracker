/** Currency and locale used for all amount and date formatting. */
export const CURRENCY = 'INR'
export const LOCALE = 'en-IN'

/** API base URL for the current mode (configs/.env.<mode>), without a trailing slash. */
export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || '/api/v1').replace(/\/+$/, '')

/**
 * The address people use to reach this app (no trailing slash): `VITE_PUBLIC_URL` when set,
 * otherwise the current origin. Set it when the origin isn't reachable from other devices,
 * e.g. `localhost` during development (see README, "Open it on your phone").
 */
export function resolvePublicUrl(configured: string | undefined, origin: string): string {
  const url = configured?.trim() || origin
  return url.replace(/\/+$/, '')
}

export const PUBLIC_URL = resolvePublicUrl(import.meta.env.VITE_PUBLIC_URL, window.location.origin)

/** Environment badge text; empty in production. */
export const ENV_LABEL = import.meta.env.VITE_ENV_LABEL ?? ''
