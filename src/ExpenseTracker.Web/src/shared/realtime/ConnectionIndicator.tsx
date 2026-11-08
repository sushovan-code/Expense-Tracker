import { useRealtimeStatus } from '@/shared/realtime/hooks'

const LABELS = {
  connected: 'Live',
  connecting: 'Connecting…',
  reconnecting: 'Reconnecting…',
  disconnected: 'Offline',
} as const

/** Header pill: realtime connection state and how many sessions are online. */
export function ConnectionIndicator() {
  const { status, online } = useRealtimeStatus()
  const title =
    status === 'connected'
      ? `Live updates on · ${online} session${online === 1 ? '' : 's'} online`
      : status === 'disconnected'
        ? 'Live updates unavailable; changes from other sessions appear after a refresh'
        : 'Connecting to live updates…'

  return (
    <span className={`live live--${status}`} title={title} role="status" aria-live="polite">
      <span className="live__dot" aria-hidden="true" />
      <span className="live__label">{LABELS[status]}</span>
      {status === 'connected' && online > 1 && <span className="live__count">{online}</span>}
    </span>
  )
}
