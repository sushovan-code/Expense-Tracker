import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  useReactTable,
  type OnChangeFn,
  type RowSelectionState,
} from '@tanstack/react-table'
import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from 'react'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'
import { formatCurrency, formatDate } from '@/shared/lib/format'
import { staggerIndex } from '@/shared/lib/motion'
import { hasErrors, toExpenseInput, validateExpense, type ExpenseDraft, type ExpenseErrors } from '../model/validation'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import type { SortDirection } from '@/shared/types'
import type { CategoryOption, Expense, ExpenseInput, ExpensePage, ExpenseSortField } from '../model/types'
import { CategoryBadge } from '@/shared/ui/CategoryBadge'
import { Checkbox } from '@/shared/ui/grid/Checkbox'
import { SortHeader } from '@/shared/ui/grid/SortHeader'
import { ariaSort } from '@/shared/ui/grid/sorting'
import { CheckIcon, PencilIcon, TrashIcon, XIcon } from '@/shared/ui/Icons'

interface ExpensesGridProps {
  data: ExpensePage
  loading: boolean
  categories: CategoryOption[]
  sortBy: ExpenseSortField
  sortDir: SortDirection
  onSort: (field: ExpenseSortField) => void
  rowSelection: RowSelectionState
  onRowSelectionChange: OnChangeFn<RowSelectionState>
  /** Persists an inline edit. Should throw (e.g. ApiError) on failure. */
  onSave: (expense: Expense, input: ExpenseInput) => Promise<void>
  onDelete: (expense: Expense) => void
  /** Inline editing (pencil button, double-click). */
  canEdit?: boolean
  /** Delete button and row selection (selection only feeds bulk delete). */
  canDelete?: boolean
}

interface EditState {
  expense: Expense
  draft: ExpenseDraft
  errors: ExpenseErrors
  saving: boolean
}

const column = createColumnHelper<Expense>()

const toDraft = (e: Expense): ExpenseDraft => ({
  description: e.description,
  amount: String(e.amount),
  date: e.date,
  categoryId: e.categoryId,
  notes: e.notes ?? '',
  isPrivate: e.isPrivate,
})

export function ExpensesGrid({
  data,
  loading,
  categories,
  sortBy,
  sortDir,
  onSort,
  rowSelection,
  onRowSelectionChange,
  onSave,
  onDelete,
  canEdit = true,
  canDelete = true,
}: ExpensesGridProps) {
  const toast = useToast()
  const [edit, setEdit] = useState<EditState | null>(null)

  const startEdit = (expense: Expense) => {
    if (!canEdit || edit?.expense.id === expense.id) return
    if (edit) logger.event('expense.inline_edit_cancelled', `Inline edit of expense ${edit.expense.id} abandoned`, { 'expense.id': edit.expense.id })
    logger.event('expense.inline_edit_started', `Inline edit of expense ${expense.id} started`, { 'expense.id': expense.id })
    setEdit({ expense, draft: toDraft(expense), errors: {}, saving: false })
  }

  const cancelEdit = () => {
    if (!edit) return
    logger.event('expense.inline_edit_cancelled', `Inline edit of expense ${edit.expense.id} cancelled`, { 'expense.id': edit.expense.id })
    setEdit(null)
  }

  const saveEdit = async () => {
    if (!edit || edit.saving) return
    const errors = validateExpense(edit.draft)
    if (hasErrors(errors)) {
      setEdit({ ...edit, errors })
      logger.event('expense.validation_failed', 'Inline edit has invalid fields', {
        'expense.id': edit.expense.id,
        'validation.fields': Object.keys(errors).join(','),
        'ui.source': 'inline',
      }, 'warn')
      toast.warning('Please fix the highlighted fields', { description: Object.values(errors)[0] })
      return
    }

    setEdit({ ...edit, errors: {}, saving: true })
    try {
      await onSave(edit.expense, toExpenseInput(edit.draft))
      setEdit(null)
    } catch (err) {
      const fieldErrors = fieldErrorsOf(err)
      setEdit((current) => current && { ...current, saving: false, errors: fieldErrors ?? {} })
      if (fieldErrors) toast.warning('Please fix the highlighted fields', { description: Object.values(fieldErrors)[0] })
      else toast.error("Couldn't save the expense", { description: errorMessage(err) })
    }
  }

  // Leave edit mode if the row disappears (deleted elsewhere, filtered out, or page changed).
  const editingId = edit?.expense.id
  useEffect(() => {
    if (editingId !== undefined && !data.items.some((e) => e.id === editingId)) setEdit(null)
  }, [data.items, editingId])

  const columns = useMemo(
    () => [
      column.display({
        id: 'select',
        header: ({ table }) => (
          <Checkbox
            checked={table.getIsAllPageRowsSelected()}
            indeterminate={table.getIsSomePageRowsSelected()}
            onChange={table.getToggleAllPageRowsSelectedHandler()}
            aria-label="Select all rows on this page"
          />
        ),
        cell: ({ row }) => (
          <Checkbox
            checked={row.getIsSelected()}
            onChange={row.getToggleSelectedHandler()}
            aria-label={`Select ${row.original.description}`}
          />
        ),
      }),
      column.accessor('date', {
        header: () => <SortHeader label="Date" field="date" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />,
        cell: (info) => <time dateTime={info.getValue()}>{formatDate(info.getValue())}</time>,
      }),
      column.accessor('description', {
        header: () => <SortHeader label="Description" field="description" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />,
        cell: ({ row }) => (
          <div className="cell-stack">
            <span className="cell-strong">{row.original.description}</span>
            {row.original.notes && <span className="cell-muted">{row.original.notes}</span>}
          </div>
        ),
      }),
      column.accessor('categoryName', {
        id: 'category',
        header: () => <SortHeader label="Category" field="category" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />,
        cell: ({ row }) => <CategoryBadge name={row.original.categoryName} color={row.original.categoryColor} />,
      }),
      column.accessor('amount', {
        header: () => <SortHeader label="Amount" field="amount" sortBy={sortBy} sortDir={sortDir} onSort={onSort} align="end" />,
        cell: (info) => <span className="cell-amount">{formatCurrency(info.getValue())}</span>,
      }),
      column.display({
        id: 'actions',
        header: () => <span className="sr-only">Actions</span>,
        cell: ({ row }) => (
          <div className="cell-actions">
            {canEdit && (
              <button type="button" className="icon-button" onClick={() => startEdit(row.original)} aria-label={`Edit ${row.original.description}`} title="Edit inline (or double-click the row)">
                <PencilIcon size={16} />
              </button>
            )}
            {canDelete && (
              <button type="button" className="icon-button icon-button--danger" onClick={() => onDelete(row.original)} aria-label={`Delete ${row.original.description}`} title="Delete">
                <TrashIcon size={16} />
              </button>
            )}
          </div>
        ),
      }),
    ].filter((c) => (c.id !== 'select' || canDelete) && (c.id !== 'actions' || canEdit || canDelete)),
    // startEdit reads `edit`; recreate columns when it changes so actions see the current state.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [sortBy, sortDir, onSort, onDelete, edit, canEdit, canDelete],
  )

  const table = useReactTable({
    data: data.items,
    columns,
    getRowId: (row) => String(row.id),
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
    manualSorting: true,
    pageCount: data.totalPages,
    enableRowSelection: canDelete,
    state: { rowSelection },
    onRowSelectionChange,
  })

  return (
    <div className="grid-wrap" aria-busy={loading}>
      {loading && <div className="grid-progress" aria-hidden="true" />}
      <table className="grid grid--expenses">
        <thead>
          {table.getHeaderGroups().map((group) => (
            <tr key={group.id}>
              {group.headers.map((header) => (
                <th
                  key={header.id}
                  className={`col-${header.column.id}`}
                  aria-sort={['date', 'description', 'category', 'amount'].includes(header.column.id)
                    ? ariaSort(header.column.id === sortBy, sortDir)
                    : undefined}
                >
                  {flexRender(header.column.columnDef.header, header.getContext())}
                </th>
              ))}
            </tr>
          ))}
        </thead>
        <tbody>
          {table.getRowModel().rows.map((row, index) =>
            edit?.expense.id === row.original.id ? (
              <EditRow
                key={row.id}
                edit={edit}
                categories={categories}
                selectable={canDelete}
                onChange={(draft) => setEdit({ ...edit, draft, errors: {} })}
                onSave={saveEdit}
                onCancel={cancelEdit}
              />
            ) : (
              <tr
                key={row.id}
                className={`grid-row${row.getIsSelected() ? ' grid-row--selected' : ''}`}
                style={staggerIndex(index)}
                onDoubleClick={() => startEdit(row.original)}
              >
                {row.getVisibleCells().map((cell) => (
                  <td key={cell.id} className={`col-${cell.column.id}`}>
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </td>
                ))}
              </tr>
            ),
          )}
        </tbody>
      </table>
    </div>
  )
}

interface EditRowProps {
  edit: EditState
  categories: CategoryOption[]
  /** Whether the grid has a selection column to line up with. */
  selectable: boolean
  onChange: (draft: ExpenseDraft) => void
  onSave: () => void
  onCancel: () => void
}

function EditRow({ edit, categories, selectable, onChange, onSave, onCancel }: EditRowProps) {
  const { draft, errors, saving, expense } = edit
  const firstInput = useRef<HTMLInputElement>(null)
  useEffect(() => firstInput.current?.focus(), [])

  const set = <K extends keyof ExpenseDraft>(key: K, value: ExpenseDraft[K]) => onChange({ ...draft, [key]: value })

  const onKeyDown = (e: KeyboardEvent<HTMLTableRowElement>) => {
    if (e.key === 'Escape') {
      e.preventDefault()
      onCancel()
    } else if (e.key === 'Enter' && !(e.target instanceof HTMLTextAreaElement) && !(e.target instanceof HTMLButtonElement)) {
      e.preventDefault()
      onSave()
    }
  }

  const invalid = (field: keyof ExpenseErrors) => ({
    'aria-invalid': errors[field] ? true : undefined,
    title: errors[field],
    className: `input input--sm${errors[field] ? ' input--invalid' : ''}`,
  })

  return (
    <tr className="grid-row grid-row--editing" onKeyDown={onKeyDown} aria-label={`Editing ${expense.description}`}>
      {selectable && <td className="col-select" />}
      <td className="col-date">
        <input type="date" value={draft.date} onChange={(e) => set('date', e.target.value)} aria-label="Date" {...invalid('date')} />
      </td>
      <td className="col-description">
        <div className="cell-stack cell-stack--edit">
          <input ref={firstInput} value={draft.description} onChange={(e) => set('description', e.target.value)} maxLength={200} aria-label="Description" {...invalid('description')} />
          <input value={draft.notes} onChange={(e) => set('notes', e.target.value)} maxLength={1000} placeholder="Notes (optional)" aria-label="Notes" {...invalid('notes')} />
        </div>
      </td>
      <td className="col-category">
        <select value={draft.categoryId} onChange={(e) => set('categoryId', e.target.value)} aria-label="Category" {...invalid('categoryId')}>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </td>
      <td className="col-amount">
        <input type="number" inputMode="decimal" min="0.01" step="0.01" value={draft.amount} onChange={(e) => set('amount', e.target.value)} aria-label="Amount" {...invalid('amount')} />
      </td>
      <td className="col-actions">
        <div className="cell-actions">
          <button type="button" className="icon-button icon-button--confirm" onClick={onSave} disabled={saving} aria-label="Save changes" title="Save (Enter)">
            <CheckIcon size={16} />
          </button>
          <button type="button" className="icon-button" onClick={onCancel} disabled={saving} aria-label="Cancel editing" title="Cancel (Esc)">
            <XIcon size={16} />
          </button>
        </div>
      </td>
    </tr>
  )
}
