import type { ReactNode } from 'react'
import { ChevronLeftIcon, ChevronRightIcon, ChevronsLeftIcon, ChevronsRightIcon } from '@/shared/ui/Icons'

interface PaginationProps {
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  pageSizeOptions?: number[]
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
  /** Extra content on the left (e.g. a filtered total). */
  summary?: ReactNode
}

export function Pagination({
  page,
  pageSize,
  totalCount,
  totalPages,
  pageSizeOptions = [10, 20, 50, 100],
  onPageChange,
  onPageSizeChange,
  summary,
}: PaginationProps) {
  const first = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const last = Math.min(page * pageSize, totalCount)
  const lastPage = Math.max(1, totalPages)

  return (
    <div className="pager">
      <div className="pager__summary">{summary}</div>

      <label className="pager__size">
        <span>Rows</span>
        <select
          className="input input--sm"
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
          aria-label="Rows per page"
        >
          {pageSizeOptions.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
      </label>

      <span className="pager__range" aria-live="polite">
        {first}–{last} of {totalCount}
      </span>

      <div className="pager__buttons">
        <button type="button" className="icon-button" onClick={() => onPageChange(1)} disabled={page <= 1} aria-label="First page">
          <ChevronsLeftIcon size={16} />
        </button>
        <button type="button" className="icon-button" onClick={() => onPageChange(page - 1)} disabled={page <= 1} aria-label="Previous page">
          <ChevronLeftIcon size={16} />
        </button>
        <span className="pager__page">
          {page} / {lastPage}
        </span>
        <button type="button" className="icon-button" onClick={() => onPageChange(page + 1)} disabled={page >= lastPage} aria-label="Next page">
          <ChevronRightIcon size={16} />
        </button>
        <button type="button" className="icon-button" onClick={() => onPageChange(lastPage)} disabled={page >= lastPage} aria-label="Last page">
          <ChevronsRightIcon size={16} />
        </button>
      </div>
    </div>
  )
}
