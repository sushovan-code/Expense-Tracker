import { useLayoutEffect, useRef, useState, type ReactNode } from 'react'
import { prefersReducedMotion } from '@/shared/lib/motion'
import { XIcon } from '@/shared/ui/Icons'

interface ModalProps {
  open: boolean
  title: string
  description?: string
  /** `lg` for dialogs with a lot of content, e.g. editors. */
  size?: 'md' | 'lg'
  onClose: () => void
  children: ReactNode
}

/**
 * Native <dialog> modal (focus trap, Escape, top layer) with enter/exit animations.
 * Content stays mounted while the exit animation plays, then the dialog closes.
 * Mark the element that should receive focus with `data-autofocus`.
 */
export function Modal({ open, title, description, size = 'md', onClose, children }: ModalProps) {
  const ref = useRef<HTMLDialogElement>(null)
  const [mounted, setMounted] = useState(open)

  // Mount content as soon as `open` turns true (adjusting state during render).
  if (open && !mounted) setMounted(true)

  // While the exit animation plays, keep showing what was on screen when it was open,
  // even though the parent has usually cleared the data the content was built from.
  const [frozen, setFrozen] = useState({ title, description, children })
  if (open && (frozen.title !== title || frozen.description !== description || frozen.children !== children)) {
    setFrozen({ title, description, children })
  }
  const content = open ? { title, description, children } : frozen

  useLayoutEffect(() => {
    const dialog = ref.current
    if (!dialog) return

    if (open) {
      if (!dialog.open) dialog.showModal()
      dialog.dataset.state = 'open'
      dialog.querySelector<HTMLElement>('[data-autofocus]')?.focus()
      return
    }

    if (!dialog.open) return
    const finish = () => {
      dialog.close()
      setMounted(false)
    }
    if (prefersReducedMotion()) return finish()

    dialog.dataset.state = 'closing'
    const onEnd = (e: AnimationEvent) => {
      if (e.target === dialog && e.animationName === 'modal-out') finish()
    }
    dialog.addEventListener('animationend', onEnd)
    return () => dialog.removeEventListener('animationend', onEnd)
  }, [open])

  return (
    <dialog
      ref={ref}
      className={size === 'lg' ? 'modal modal--lg' : 'modal'}
      aria-labelledby="modal-title"
      onCancel={(e) => {
        // Escape: run the exit animation instead of closing instantly.
        e.preventDefault()
        onClose()
      }}
      onClick={(e) => {
        // A click on the dialog element itself (not its content) is a click on the backdrop.
        if (e.target === ref.current) onClose()
      }}
    >
      {mounted && (
        <div className="modal__content">
          <div className="modal__header">
            <div>
              <h2 id="modal-title" className="modal__title">
                {content.title}
              </h2>
              {content.description && <p className="modal__description">{content.description}</p>}
            </div>
            <button type="button" className="icon-button" onClick={onClose} aria-label="Close">
              <XIcon />
            </button>
          </div>
          {content.children}
        </div>
      )}
    </dialog>
  )
}
