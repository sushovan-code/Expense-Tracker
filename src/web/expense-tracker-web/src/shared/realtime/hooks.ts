import { useEffect, useLayoutEffect, useRef, useSyncExternalStore } from 'react'
import { getSnapshot, subscribeEvent, subscribeStatus, type RealtimeEvents } from './connection'

/** Connection status and how many sessions are online. */
export function useRealtimeStatus() {
  return useSyncExternalStore(subscribeStatus, getSnapshot)
}

/** Runs `handler` for every `name` message; the latest handler is always used. */
export function useRealtimeEvent<K extends keyof RealtimeEvents>(
  name: K,
  handler: (payload: RealtimeEvents[K]) => void,
) {
  const handlerRef = useRef(handler)
  useLayoutEffect(() => {
    handlerRef.current = handler
  })
  useEffect(() => subscribeEvent(name, (payload) => handlerRef.current(payload)), [name])
}
