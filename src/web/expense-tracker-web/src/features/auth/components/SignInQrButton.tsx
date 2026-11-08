import { useState } from 'react'
import { PUBLIC_URL } from '@/shared/config/env'
import { InfoIcon, QrCodeIcon, WalletIcon } from '@/shared/ui/Icons'
import { Modal } from '@/shared/ui/Modal'
import { QrCode } from '@/shared/ui/QrCode'

/** Path of the sign-in page that the code opens. */
const SIGN_IN_PATH = '/login'

/** Host names a phone can't reach, because they point back at the phone itself. */
const LOOPBACK_HOSTS = new Set(['localhost', '127.0.0.1', '[::1]'])

/**
 * Button on the sign-in card that shows a QR code of this site's sign-in page,
 * so someone can open the app on their phone and sign in there as usual.
 */
export function SignInQrButton({ publicUrl = PUBLIC_URL }: { publicUrl?: string }) {
  const [open, setOpen] = useState(false)
  const signInUrl = new URL(SIGN_IN_PATH, publicUrl)
  const isLoopback = LOOPBACK_HOSTS.has(signInUrl.hostname)

  return (
    <>
      <button type="button" className="button button--ghost qr-trigger" onClick={() => setOpen(true)}>
        <QrCodeIcon size={16} /> Open on your phone
      </button>

      <Modal open={open} title="Open on your phone" onClose={() => setOpen(false)}>
        <div className="qr-panel">
          <p className="qr-panel__caption">
            <strong>Scan to open Expense Tracker</strong>
            <span>Point your phone's camera at the code, then sign in as usual.</span>
          </p>
          <QrCode
            value={signInUrl.href}
            logo={
              <span className="brand__mark qr-panel__mark">
                <WalletIcon size={22} />
              </span>
            }
          />
          <p className="qr-panel__url">{signInUrl.href}</p>
          {isLoopback && (
            <p className="auth-alert auth-alert--info qr-panel__hint" role="status">
              <InfoIcon size={16} />
              <span>
                This address only works on this computer. To scan from a phone on the same Wi-Fi, set{' '}
                <code>VITE_PUBLIC_URL</code> to this computer's network address (see the README).
              </span>
            </p>
          )}
        </div>
      </Modal>
    </>
  )
}
