import type { CSSProperties } from 'react'

export const prefersReducedMotion = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches

/** Inline style that sets the stagger index used by `.stagger` children in CSS. */
export const staggerIndex = (i: number) => ({ '--i': Math.min(i, 12) }) as CSSProperties
