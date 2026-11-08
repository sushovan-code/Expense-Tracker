import type { CSSProperties, ReactNode } from 'react'

interface StatCardProps {
  label: string
  value: ReactNode
  hint?: ReactNode
  icon: ReactNode
  /** Accent color for the icon chip. */
  tint: string
  style?: CSSProperties
}

export function StatCard({ label, value, hint, icon, tint, style }: StatCardProps) {
  return (
    <div className="card stat-card" style={{ ...style, '--tint': tint } as CSSProperties}>
      <div className="stat-card__top">
        <span className="stat-card__label">{label}</span>
        <span className="stat-card__icon">{icon}</span>
      </div>
      <div className="stat-card__value">{value}</div>
      {hint && <div className="stat-card__hint">{hint}</div>}
    </div>
  )
}
