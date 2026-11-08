import { createContext, useContext } from 'react'

export type ToastKind = 'success' | 'error' | 'warning' | 'info'

export interface ToastAction {
  label: string
  onClick: () => void
}

export interface ToastOptions {
  description?: string
  /** Auto-dismiss delay in ms. Defaults depend on kind; errors stay longer. */
  duration?: number
  action?: ToastAction
}

export interface ToastApi {
  success: (title: string, options?: ToastOptions) => number
  error: (title: string, options?: ToastOptions) => number
  warning: (title: string, options?: ToastOptions) => number
  info: (title: string, options?: ToastOptions) => number
  dismiss: (id: number) => void
}

export const ToastContext = createContext<ToastApi | null>(null)

export function useToast(): ToastApi {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast must be used inside <ToastProvider>')
  return ctx
}
