import type { SortDirection } from '@/shared/types'

/** aria-sort value for the <th> wrapping a SortHeader. */
export const ariaSort = (active: boolean, dir: SortDirection) =>
  active ? (dir === 'asc' ? 'ascending' : 'descending') : 'none'
