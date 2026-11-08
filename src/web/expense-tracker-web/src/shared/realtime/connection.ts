import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
  type ILogger,
} from '@microsoft/signalr'
import { API_BASE_URL } from '@/shared/config/env'
import { CLIENT_ID } from '@/shared/lib/clientId'
import { logger } from '@/shared/telemetry'

// ---- Server → client messages (mirror server/ExpenseTracker.Api/Realtime/RealtimeContracts.cs) ----

export type ChangeAction = 'created' | 'updated' | 'deleted'

export interface ChangeNotice {
  action: ChangeAction
  ids: string[]
  originClientId: string | null
}

export interface BudgetAlert {
  categoryId: string
  categoryName: string
  budget: number
  spent: number
  year: number
  month: number
}

export interface PresenceUpdate {
  online: number
}

export interface RealtimeEvents {
  ExpensesChanged: ChangeNotice
  CategoriesChanged: ChangeNotice
  BudgetExceeded: BudgetAlert
  PresenceChanged: PresenceUpdate
  /** Local event: the connection came back, so anything may have been missed. */
  Resynced: void
}

export type ConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

export interface RealtimeSnapshot {
  status: ConnectionStatus
  online: number
}

const SERVER_EVENTS = ['ExpensesChanged', 'CategoriesChanged', 'BudgetExceeded', 'PresenceChanged'] as const

/** `/api` → `/hubs/expenses` (same origin, proxied in dev); absolute API URL → same host. */
function hubUrl(): string {
  const base = /^https?:\/\//.test(API_BASE_URL) ? new URL(API_BASE_URL).origin : ''
  return `${base}/hubs/expenses?clientId=${encodeURIComponent(CLIENT_ID)}`
}

/** Routes SignalR's own diagnostics into the app logger (warnings and errors only). */
const signalRLogger: ILogger = {
  log(level, message) {
    if (level >= LogLevel.Error) logger.error(`SignalR: ${message}`, undefined, { 'signalr.log_level': LogLevel[level] })
    else if (level >= LogLevel.Warning) logger.warn(`SignalR: ${message}`, { 'signalr.log_level': LogLevel[level] })
  },
}

// ---- Connection singleton -------------------------------------------------------------------

let connection: HubConnection | null = null
let snapshot: RealtimeSnapshot = { status: 'disconnected', online: 0 }
let retryTimer: ReturnType<typeof setTimeout> | undefined
let failedAttempts = 0
/** Between startRealtime and stopRealtime: keep (re)connecting. */
let wanted = false
let accessTokenFactory: () => string | null = () => null

const statusListeners = new Set<() => void>()
const eventListeners = new Map<keyof RealtimeEvents, Set<(payload: never) => void>>()

function setSnapshot(next: Partial<RealtimeSnapshot>) {
  snapshot = { ...snapshot, ...next }
  statusListeners.forEach((listener) => listener())
}

function dispatch<K extends keyof RealtimeEvents>(name: K, payload: RealtimeEvents[K]) {
  eventListeners.get(name)?.forEach((listener) => (listener as (p: RealtimeEvents[K]) => void)(payload))
}

function describe(name: string, payload: unknown): string {
  const notice = payload as Partial<ChangeNotice>
  if (notice?.action && Array.isArray(notice.ids)) return `${name}: ${notice.ids.length} ${notice.action}`
  return name
}

function createConnection(): HubConnection {
  const hub = new HubConnectionBuilder()
    // Read on every (re)connect, so a reconnect after the server closes an expired connection uses the renewed token.
    .withUrl(hubUrl(), { accessTokenFactory: () => accessTokenFactory() ?? '' })
    .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
    .configureLogging(signalRLogger)
    .build()

  for (const name of SERVER_EVENTS) {
    hub.on(name, (payload: RealtimeEvents[typeof name]) => {
      const notice = payload as Partial<ChangeNotice>
      logger.debug(`Realtime message received: ${describe(name, payload)}`, {
        'event.name': 'realtime.message_received',
        'signalr.message': name,
        'realtime.action': notice?.action,
        'realtime.item_count': Array.isArray(notice?.ids) ? notice.ids.length : undefined,
        'realtime.from_this_tab': notice?.originClientId === CLIENT_ID,
      })
      if (name === 'PresenceChanged') setSnapshot({ online: (payload as PresenceUpdate).online })
      dispatch(name, payload)
    })
  }

  hub.onreconnecting((error) => {
    setSnapshot({ status: 'reconnecting' })
    logger.event('realtime.reconnecting', 'Realtime connection lost; reconnecting', { 'error.message': error?.message }, 'warn')
  })

  hub.onreconnected((connectionId) => {
    setSnapshot({ status: 'connected' })
    logger.event('realtime.reconnected', 'Realtime connection restored', { 'signalr.connection_id': connectionId })
    dispatch('Resynced', undefined)
  })

  hub.onclose((error) => {
    setSnapshot({ status: 'disconnected', online: 0 })
    if (!wanted) return
    logger.event('realtime.disconnected', 'Realtime connection closed', { 'error.message': error?.message }, 'warn')
    scheduleStart()
  })

  return hub
}

function scheduleStart() {
  clearTimeout(retryTimer)
  if (!wanted) return
  const delay = Math.min(30_000, 1_000 * 2 ** failedAttempts)
  retryTimer = setTimeout(() => void startRealtime(accessTokenFactory), delay)
}

/**
 * Connects (once) as the signed-in user and keeps retrying with exponential backoff until it succeeds.
 * The hub requires authentication, so call this after sign-in and {@link stopRealtime} on sign-out.
 */
export async function startRealtime(getAccessToken: () => string | null): Promise<void> {
  accessTokenFactory = getAccessToken
  wanted = true
  connection ??= createConnection()
  if (connection.state !== HubConnectionState.Disconnected) return

  setSnapshot({ status: 'connecting' })
  try {
    await connection.start()
    const wasRetry = failedAttempts > 0
    failedAttempts = 0
    setSnapshot({ status: 'connected' })
    logger.event('realtime.connected', 'Realtime connected', { 'signalr.connection_id': connection.connectionId ?? undefined })
    if (wasRetry) dispatch('Resynced', undefined)
  } catch (error) {
    if (!wanted) return
    failedAttempts++
    setSnapshot({ status: 'disconnected' })
    const delay = Math.min(30_000, 1_000 * 2 ** failedAttempts)
    logger.event(
      'realtime.connect_failed',
      `Realtime connection failed (attempt ${failedAttempts}); retrying in ${Math.round(delay / 1000)} s`,
      { 'error.message': error instanceof Error ? error.message : String(error), 'realtime.attempt': failedAttempts },
      'warn',
    )
    scheduleStart()
  }
}

/** Disconnects and stops retrying (on sign-out). */
export async function stopRealtime(): Promise<void> {
  wanted = false
  failedAttempts = 0
  clearTimeout(retryTimer)
  if (connection && connection.state !== HubConnectionState.Disconnected) await connection.stop()
  setSnapshot({ status: 'disconnected', online: 0 })
}

// ---- Subscriptions (used by the hooks in ./hooks.ts) ----------------------------------------

export function subscribeStatus(listener: () => void) {
  statusListeners.add(listener)
  return () => statusListeners.delete(listener)
}

export const getSnapshot = () => snapshot

export function subscribeEvent<K extends keyof RealtimeEvents>(name: K, listener: (payload: RealtimeEvents[K]) => void) {
  let listeners = eventListeners.get(name)
  if (!listeners) eventListeners.set(name, (listeners = new Set()))
  listeners.add(listener as (payload: never) => void)
  return () => {
    listeners.delete(listener as (payload: never) => void)
  }
}

export const isFromThisTab = (notice: ChangeNotice) => notice.originClientId === CLIENT_ID
