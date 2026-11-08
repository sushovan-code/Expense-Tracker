import { useState } from 'react'
import { PUBLIC_URL } from '@/shared/config/env'
import { QrCodeIcon } from '@/shared/ui/Icons'
import { Modal } from '@/shared/ui/Modal'
import { QrCode } from '@/shared/ui/QrCode'

/** Path of the sign-in page that the code opens. */
const SIGN_IN_PATH = '/login'

/**
 * Icon button on the sign-in card that shows a QR code of this site's sign-in page,
 * so someone can open the app on their phone and sign in there as usual.
 */
export function SignInQrButton({ publicUrl = PUBLIC_URL }: { publicUrl?: string }) {
  const [open, setOpen] = useState(false)
  const signInUrl = new URL(SIGN_IN_PATH, publicUrl).href

  return (
    <>
      <button
        type="button"
        className="icon-button auth-card__qr"
        onClick={() => setOpen(true)}
        aria-label="Open on your phone"
        title="Open on your phone"
      >
        <QrCodeIcon />
      </button>

      <Modal
        open={open}
        title="Open on your phone"
        description="Scan with your phone's camera to open the sign-in page there."
        onClose={() => setOpen(false)}
      >
        <div className="qr-panel">
          <QrCode value={signInUrl} />
          <p className="qr-panel__url">{signInUrl}</p>
        </div>
      </Modal>
    </>
  )
}
