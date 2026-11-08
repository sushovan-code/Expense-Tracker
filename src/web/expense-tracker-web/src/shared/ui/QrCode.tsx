import { create } from 'qrcode'
import { useMemo, type ReactNode } from 'react'

/** Blank modules around the code; scanners need a quiet zone of 4. */
const QUIET_ZONE = 4

interface QrCodeProps {
  /** Text to encode, usually a URL. */
  value: string
  /** Optional mark drawn over the centre. The code then uses the highest error correction, so it still scans. */
  logo?: ReactNode
}

/** One SVG path with a 1×1 square per dark module, offset by the quiet zone. */
function modulesPath(value: string, hasLogo: boolean): { path: string; dimension: number } {
  // H recovers up to 30% of the code; the logo covers about 5%.
  const { modules } = create(value, { errorCorrectionLevel: hasLogo ? 'H' : 'M' })
  let path = ''
  for (let row = 0; row < modules.size; row++) {
    for (let column = 0; column < modules.size; column++) {
      if (modules.get(row, column)) path += `M${column + QUIET_ZONE} ${row + QUIET_ZONE}h1v1h-1z`
    }
  }
  return { path, dimension: modules.size + QUIET_ZONE * 2 }
}

/**
 * A scannable QR code as SVG. It always uses solid dark-on-light QR tokens, in both themes:
 * phone cameras need high, opaque contrast, so the translucent surface colours aren't used here.
 * It scales to its container's width.
 */
export function QrCode({ value, logo }: QrCodeProps) {
  const { path, dimension } = useMemo(() => modulesPath(value, logo !== undefined), [value, logo])

  return (
    <div className="qr-code">
      <svg viewBox={`0 0 ${dimension} ${dimension}`} shapeRendering="crispEdges" role="img" aria-label={`QR code for ${value}`}>
        <rect width={dimension} height={dimension} fill="var(--qr-paper)" />
        <path d={path} fill="var(--qr-ink)" />
      </svg>
      {logo && (
        <span className="qr-code__logo" aria-hidden="true">
          {logo}
        </span>
      )}
    </div>
  )
}
