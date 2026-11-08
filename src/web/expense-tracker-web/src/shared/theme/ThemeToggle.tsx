import type { CSSProperties, MouseEvent, ReactNode } from 'react'
import { flushSync } from 'react-dom'
import { prefersReducedMotion } from '@/shared/lib/motion'
import { useTheme, type ThemeMode } from '@/shared/theme/theme'
import { MonitorIcon, MoonIcon, SunIcon } from '@/shared/ui/Icons'

const OPTIONS: { mode: ThemeMode; label: string; icon: ReactNode }[] = [
  { mode: 'light', label: 'Light', icon: <SunIcon size={16} /> },
  { mode: 'dark', label: 'Dark', icon: <MoonIcon size={16} /> },
  { mode: 'system', label: 'System', icon: <MonitorIcon size={16} /> },
]

export function ThemeToggle() {
  const { mode, setMode } = useTheme()
  const index = OPTIONS.findIndex((o) => o.mode === mode)

  const select = (next: ThemeMode, e: MouseEvent<HTMLButtonElement>) => {
    if (next === mode) return
    if (!document.startViewTransition || prefersReducedMotion()) {
      setMode(next)
      return
    }

    // Reveal the new theme as a circle growing from the clicked button.
    const { clientX: x, clientY: y } = e
    const radius = Math.hypot(Math.max(x, innerWidth - x), Math.max(y, innerHeight - y))
    const transition = document.startViewTransition(() => flushSync(() => setMode(next)))
    transition.ready.then(() => {
      document.documentElement.animate(
        { clipPath: [`circle(0px at ${x}px ${y}px)`, `circle(${radius}px at ${x}px ${y}px)`] },
        { duration: 600, easing: 'cubic-bezier(0.4, 0, 0.2, 1)', pseudoElement: '::view-transition-new(root)' },
      )
    })
  }

  return (
    <div
      className="theme-toggle"
      role="radiogroup"
      aria-label="Theme"
      style={{ '--index': index } as CSSProperties}
    >
      <span className="theme-toggle__thumb" aria-hidden="true" />
      {OPTIONS.map((option) => (
        <button
          key={option.mode}
          type="button"
          role="radio"
          aria-checked={mode === option.mode}
          aria-label={`${option.label} theme`}
          title={`${option.label} theme`}
          className="theme-toggle__option"
          onClick={(e) => select(option.mode, e)}
        >
          {option.icon}
        </button>
      ))}
    </div>
  )
}
