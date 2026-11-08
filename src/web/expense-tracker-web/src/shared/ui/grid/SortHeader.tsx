import type { SortDirection } from '@/shared/types'
import { ArrowDownIcon, ArrowUpIcon, ArrowUpDownIcon } from '@/shared/ui/Icons'

interface SortHeaderProps<F extends string> {
  label: string
  field: F
  sortBy: F
  sortDir: SortDirection
  onSort: (field: F) => void
  align?: 'start' | 'end'
}

/** Column header button for server-side sorting; announces the current order to screen readers. */
export function SortHeader<F extends string>({ label, field, sortBy, sortDir, onSort, align = 'start' }: SortHeaderProps<F>) {
  const active = field === sortBy
  return (
    <button
      type="button"
      className={`sort-header sort-header--${align}${active ? ' sort-header--active' : ''}`}
      onClick={() => onSort(field)}
      aria-label={`Sort by ${label}${active ? (sortDir === 'asc' ? ', currently ascending' : ', currently descending') : ''}`}
    >
      {label}
      {active ? (
        sortDir === 'asc' ? <ArrowUpIcon size={14} /> : <ArrowDownIcon size={14} />
      ) : (
        <ArrowUpDownIcon size={14} />
      )}
    </button>
  )
}
