import { create } from 'qrcode'
import { useMemo } from 'react'

/** Blank modules around the code; scanners need a quiet zone of 4. */
const QUIET_ZONE = 4

interface QrCodeProps {
  /** Text to encode, usually a URL. */
  value: string
  /** Rendered width and height in pixels. */
  size?: number
}

/** One SVG path with a 1×1 square per dark module, offset by the quiet zone. */
function modulesPath(value: string): { path: string; dimension: number } {
  const { modules } = create(value, { errorCorrectionLevel: 'M' })
  let path = ''
  for (let row = 0; row < modules.size; row++) {
    for (let column = 0; column < modules.size; column++) {
      if (modules.get(row, column)) path += `M${column + QUIET_ZONE} ${row + QUIET_ZONE}h1v1h-1z`
    }
  }
  return { path, dimension: modules.size + QUIET_ZONE * 2 }
}

/**
 * A QR code drawn as SVG in theme tokens (text colour on the surface colour), so it follows
 * light and dark themes. Phone camera apps read both normal and inverted codes.
 */
export function QrCode({ value, size = 200 }: QrCodeProps) {
  const { path, dimension } = useMemo(() => modulesPath(value), [value])

  return (
    <svg
      className="qr-code"
      width={size}
      height={size}
      viewBox={`0 0 ${dimension} ${dimension}`}
      shapeRendering="crispEdges"
      role="img"
      aria-label={`QR code for ${value}`}
    >
      <rect width={dimension} height={dimension} fill="var(--surface)" />
      <path d={path} fill="var(--text)" />
    </svg>
  )
}
