import type { CSSProperties } from 'react'

export function CategoryBadge({ name, color }: { name: string; color: string }) {
  return (
    <span className="badge" style={{ '--badge-color': color } as CSSProperties}>
      <span className="badge__dot" />
      {name}
    </span>
  )
}
