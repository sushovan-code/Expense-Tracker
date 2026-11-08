import { useState, type ReactNode } from 'react'
import { AlertTriangleIcon } from '@/shared/ui/Icons'
import { Modal } from '@/shared/ui/Modal'

interface ConfirmDialogProps {
  open: boolean
  title: string
  message: ReactNode
  confirmLabel?: string
  busyLabel?: string
  /** Should handle its own errors (e.g. by raising a toast); the dialog closes when it settles. */
  onConfirm: () => Promise<void>
  onClose: () => void
}

export function ConfirmDialog({ open, title, message, confirmLabel = 'Delete', busyLabel = 'Deleting…', onConfirm, onClose }: ConfirmDialogProps) {
  const [busy, setBusy] = useState(false)

  const confirm = async () => {
    setBusy(true)
    try {
      await onConfirm()
    } finally {
      setBusy(false)
      onClose()
    }
  }

  return (
    <Modal open={open} title={title} onClose={onClose}>
      <div className="confirm">
        <span className="confirm__icon">
          <AlertTriangleIcon size={22} />
        </span>
        <p className="confirm__message">{message}</p>
      </div>
      <div className="form-actions">
        <button type="button" className="button button--ghost" onClick={onClose} data-autofocus>
          Cancel
        </button>
        <button type="button" className="button button--danger" onClick={confirm} disabled={busy}>
          {busy ? busyLabel : confirmLabel}
        </button>
      </div>
    </Modal>
  )
}
