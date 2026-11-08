import { useCallback, useLayoutEffect, useMemo, useRef, useState, type CSSProperties, type ReactNode } from 'react'
import { AlertTriangleIcon, CheckCircleIcon, InfoIcon, XCircleIcon, XIcon } from '@/shared/ui/Icons'
import { logger } from '@/shared/telemetry'
import { ToastContext, type ToastApi, type ToastKind, type ToastOptions } from '@/shared/toast/toast'

interface ToastItem extends ToastOptions {
  id: number
  kind: ToastKind
  title: string
  leaving: boolean
}

const DEFAULT_DURATION: Record<ToastKind, number> = {
  success: 4000,
  info: 4000,
  warning: 6000,
  error: 7000,
}
const MAX_VISIBLE = 4
const EXIT_MS = 260

const ICONS: Record<ToastKind, ReactNode> = {
  success: <CheckCircleIcon size={20} />,
  error: <XCircleIcon size={20} />,
  warning: <AlertTriangleIcon size={20} />,
  info: <InfoIcon size={20} />,
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([])
  const nextId = useRef(1)
  const toasterRef = useRef<HTMLElement>(null)

  // Modal dialogs live in the browser's top layer, above any z-index. The toaster is a
  // popover so it can join that layer; when a toast arrives while a modal is open, it is
  // re-shown so it stacks above the modal. (Its CSS keeps it displayed while toggling,
  // so running toast animations and timers aren't restarted.)
  const newestId = toasts.at(-1)?.id
  useLayoutEffect(() => {
    const toaster = toasterRef.current
    if (!toaster || typeof toaster.showPopover !== 'function') return
    const isOpen = toaster.matches(':popover-open')
    if (isOpen && document.querySelector('dialog[open]')) toaster.hidePopover()
    if (!toaster.matches(':popover-open')) toaster.showPopover()
  }, [newestId])

  const dismiss = useCallback((id: number) => {
    setToasts((list) => list.map((t) => (t.id === id ? { ...t, leaving: true } : t)))
    setTimeout(() => setToasts((list) => list.filter((t) => t.id !== id)), EXIT_MS)
  }, [])

  const push = useCallback((kind: ToastKind, title: string, options: ToastOptions = {}) => {
    const id = nextId.current++
    // Every notification the user sees is also a log record, at the matching severity.
    // Titles only: descriptions can contain user-entered text (e.g. expense descriptions).
    const attributes = { 'toast.kind': kind }
    const message = `Toast: ${title}`
    if (kind === 'error') logger.error(message, undefined, attributes)
    else if (kind === 'warning') logger.warn(message, attributes)
    else logger.info(message, attributes)
    setToasts((list) => [...list.slice(-(MAX_VISIBLE - 1)), { id, kind, title, leaving: false, ...options }])
    return id
  }, [])

  const api = useMemo<ToastApi>(
    () => ({
      success: (title, options) => push('success', title, options),
      error: (title, options) => push('error', title, options),
      warning: (title, options) => push('warning', title, options),
      info: (title, options) => push('info', title, options),
      dismiss,
    }),
    [push, dismiss],
  )

  return (
    <ToastContext.Provider value={api}>
      {children}
      <section ref={toasterRef} className="toaster" aria-label="Notifications" popover="manual">
        {toasts.map((toast) => (
          <Toast key={toast.id} toast={toast} onDismiss={() => dismiss(toast.id)} />
        ))}
      </section>
    </ToastContext.Provider>
  )
}

function Toast({ toast, onDismiss }: { toast: ToastItem; onDismiss: () => void }) {
  const duration = toast.duration ?? DEFAULT_DURATION[toast.kind]
  const urgent = toast.kind === 'error' || toast.kind === 'warning'

  return (
    <div
      className={`toast toast--${toast.kind}`}
      data-leaving={toast.leaving || undefined}
      role={urgent ? 'alert' : 'status'}
      aria-live={urgent ? 'assertive' : 'polite'}
    >
      <span className="toast__icon">{ICONS[toast.kind]}</span>
      <div className="toast__body">
        <p className="toast__title">{toast.title}</p>
        {toast.description && <p className="toast__description">{toast.description}</p>}
      </div>
      {toast.action && (
        <button
          type="button"
          className="toast__action"
          onClick={() => {
            toast.action!.onClick()
            onDismiss()
          }}
        >
          {toast.action.label}
        </button>
      )}
      <button type="button" className="toast__close" onClick={onDismiss} aria-label="Dismiss notification">
        <XIcon size={14} />
      </button>
      {/* The progress bar's animation doubles as the timer: it pauses on hover/focus, and ends → dismiss. */}
      <span
        className="toast__progress"
        style={{ '--duration': `${duration}ms` } as CSSProperties}
        onAnimationEnd={onDismiss}
      />
    </div>
  )
}
