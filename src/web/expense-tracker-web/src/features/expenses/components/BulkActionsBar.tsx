import { TrashIcon, XIcon } from '@/shared/ui/Icons'
import type { ExpensesViewModel } from '../viewModels/useExpensesViewModel'

/** Appears while rows are selected: selection count, clear, and delete (which asks for confirmation). */
export function BulkActionsBar({ selection }: { selection: ExpensesViewModel['selection'] }) {
  if (selection.count === 0) return null

  return (
    <div className="bulk-bar" role="region" aria-label="Bulk actions">
      <span className="bulk-bar__count">
        {selection.count} selected
        {selection.spansPages && <span className="muted"> (across pages)</span>}
      </span>
      <button type="button" className="button button--ghost button--sm" onClick={selection.clear}>
        <XIcon size={14} /> Clear selection
      </button>
      <button type="button" className="button button--danger button--sm" onClick={selection.requestDelete}>
        <TrashIcon size={14} /> Delete selected
      </button>
    </div>
  )
}
