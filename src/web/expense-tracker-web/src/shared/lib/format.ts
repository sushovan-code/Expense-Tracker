import { CURRENCY, LOCALE } from '@/shared/config/env'
import type { IsoDate } from '@/shared/types'

export interface YearMonth {
  year: number
  /** 1–12 */
  month: number
}

const currencyFormat = new Intl.NumberFormat(LOCALE, { style: 'currency', currency: CURRENCY })
const compactCurrencyFormat = new Intl.NumberFormat(LOCALE, {
  style: 'currency',
  currency: CURRENCY,
  notation: 'compact',
  maximumFractionDigits: 1,
})
const dateFormat = new Intl.DateTimeFormat(LOCALE, { day: 'numeric', month: 'short', year: 'numeric' })

export const formatCurrency = (value: number) => currencyFormat.format(value)
export const formatCompactCurrency = (value: number) => compactCurrencyFormat.format(value)

const pad = (n: number) => String(n).padStart(2, '0')

/** Local calendar date as `YYYY-MM-DD` (no timezone shift). */
export const toIsoDate = (d: Date): IsoDate => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`

export const today = (): IsoDate => toIsoDate(new Date())

export function parseIsoDate(iso: IsoDate): Date {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d)
}

export const formatDate = (iso: IsoDate) => dateFormat.format(parseIsoDate(iso))

const timestampFormat = new Intl.DateTimeFormat(LOCALE, { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })

/** A point in time (ISO 8601 with offset, e.g. from DateTimeOffset) in the user's local time. */
export const formatTimestamp = (iso: string) => timestampFormat.format(new Date(iso))

export function currentMonth(): YearMonth {
  const now = new Date()
  return { year: now.getFullYear(), month: now.getMonth() + 1 }
}

export function addMonths({ year, month }: YearMonth, delta: number): YearMonth {
  const d = new Date(year, month - 1 + delta, 1)
  return { year: d.getFullYear(), month: d.getMonth() + 1 }
}

export function monthRange({ year, month }: YearMonth): { from: IsoDate; to: IsoDate } {
  return { from: `${year}-${pad(month)}-01`, to: toIsoDate(new Date(year, month, 0)) }
}

export function formatMonth({ year, month }: YearMonth, style: 'long' | 'short' = 'long'): string {
  return new Intl.DateTimeFormat(LOCALE, { month: style, year: style === 'long' ? 'numeric' : '2-digit' }).format(
    new Date(year, month - 1, 1),
  )
}

export const sameMonth = (a: YearMonth, b: YearMonth) => a.year === b.year && a.month === b.month
