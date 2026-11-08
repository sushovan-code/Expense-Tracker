/** Currency and locale used for all amount and date formatting. */
export const CURRENCY = 'INR'
export const LOCALE = 'en-IN'

/** API base URL for the current mode (configs/.env.<mode>), without a trailing slash. */
export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/+$/, '')

/** Environment badge text; empty in production. */
export const ENV_LABEL = import.meta.env.VITE_ENV_LABEL ?? ''
