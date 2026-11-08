import { useEffect, useRef, useState } from 'react'
import { prefersReducedMotion } from '@/shared/lib/motion'

const easeOutCubic = (t: number) => 1 - (1 - t) ** 3

/** Animates from the previous value to `target`, for numbers that "tick" into place. */
export function useCountUp(target: number, durationMs = 800): number {
  const [value, setValue] = useState(target)
  const fromRef = useRef(0)

  useEffect(() => {
    const from = fromRef.current
    fromRef.current = target
    if (prefersReducedMotion() || from === target) {
      setValue(target)
      return
    }

    let frame = 0
    const start = performance.now()
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / durationMs)
      setValue(from + (target - from) * easeOutCubic(t))
      if (t < 1) frame = requestAnimationFrame(tick)
    }
    frame = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(frame)
  }, [target, durationMs])

  return value
}
