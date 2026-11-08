import { createColumnHelper, flexRender, getCoreRowModel, useReactTable } from '@tanstack/react-table'
import { useEffect, useMemo, useRef, useState, type CSSProperties, type KeyboardEvent } from 'react'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'
import { formatCurrency } from '@/shared/lib/format'
import { staggerIndex } from '@/shared/lib/motion'
import { hasErrors, toCategoryInput, validateCategory, type CategoryDraft, type CategoryErrors } from '../model/validation'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import type { PagedResponse, SortDirection } from '@/shared/types'
import type { Category, CategoryInput, CategorySortField } from '../model/types'
import { SortHeader } from '@/shared/ui/grid/SortHeader'
import { ariaSort } from '@/shared/ui/grid/sorting'
import { CheckIcon, PencilIcon, TrashIcon, XIcon } from '@/shared/ui/Icons'

interface CategoriesGridProps {
  data: PagedResponse<Category>
  loading: boolean
  sortBy: CategorySortField
  sortDir: SortDirection
  onSort: (field: CategorySortField) => void
  /** Persists an inline edit. Should throw (e.g. ApiError) on failure. */
  onSave: (category: Category, input: CategoryInput) => Promise<void>
  onDelete: (category: Category) => void
  canEdit?: boolean
  canDelete?: boolean
}

interface EditState {
  category: Category
  draft: CategoryDraft
  errors: CategoryErrors
  saving: boolean
}

const column = createColumnHelper<Category>()

const toDraft = (c: Category): CategoryDraft => ({
  name: c.name,
  color: c.color,
  monthlyBudget: c.monthlyBudget != null ? String(c.monthlyBudget) : '',
})

export function CategoriesGrid({ data, loading, sortBy, sortDir, onSort, onSave, onDelete, canEdit = true, canDelete = true }: CategoriesGridProps) {
  const toast = useToast()
  const [edit, setEdit] = useState<EditState | null>(null)

  const startEdit = (category: Category) => {
    if (!canEdit || edit?.category.id === category.id) return
    logger.event('category.inline_edit_started', `Inline edit of category ${category.id} started`, { 'category.id': category.id })
    setEdit({ category, draft: toDraft(category), errors: {}, saving: false })
  }

  const cancelEdit = () => {
    if (!edit) return
    logger.event('category.inline_edit_cancelled', `Inline edit of category ${edit.category.id} cancelled`, { 'category.id': edit.category.id })
    setEdit(null)
  }

  const saveEdit = async () => {
    if (!edit || edit.saving) return
    const errors = validateCategory(edit.draft)
    if (hasErrors(errors)) {
      setEdit({ ...edit, errors })
      logger.event('category.validation_failed', 'Inline category edit has invalid fields', {
        'category.id': edit.category.id,
        'validation.fields': Object.keys(errors).join(','),
        'ui.source': 'inline',
      }, 'warn')
      toast.warning('Please fix the highlighted fields', { description: Object.values(errors)[0] })
      return
    }
    setEdit({ ...edit, errors: {}, saving: true })
    try {
      await onSave(edit.category, toCategoryInput(edit.draft))
      setEdit(null)
    } catch (err) {
      const fieldErrors = fieldErrorsOf(err)
      setEdit((current) => current && { ...current, saving: false, errors: fieldErrors ?? {} })
      if (fieldErrors) toast.warning('Please fix the highlighted fields', { description: Object.values(fieldErrors)[0] })
      else toast.error("Couldn't save the category", { description: errorMessage(err) })
    }
  }

  const editingId = edit?.category.id
  useEffect(() => {
    if (editingId !== undefined && !data.items.some((c) => c.id === editingId)) setEdit(null)
  }, [data.items, editingId])

  const columns = useMemo(
    () => [
      column.accessor('name', {
        header: () => <SortHeader label="Category" field="name" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />,
        cell: ({ row }) => (
          <div className="cell-category">
            <span className="category-swatch" style={{ '--swatch': row.original.color } as CSSProperties} />
            <span className="cell-strong">{row.original.name}</span>
          </div>
        ),
      }),
      column.accessor('monthlyBudget', {
        id: 'budget',
        header: () => <SortHeader label="Monthly budget" field="budget" sortBy={sortBy} sortDir={sortDir} onSort={onSort} align="end" />,
        cell: (info) =>
          info.getValue() != null ? (
            <span className="cell-amount">{formatCurrency(info.getValue()!)}</span>
          ) : (
            <span className="cell-muted">No limit</span>
          ),
      }),
      column.accessor('expenseCount', {
        id: 'expenses',
        header: () => <SortHeader label="Expenses" field="expenses" sortBy={sortBy} sortDir={sortDir} onSort={onSort} align="end" />,
        cell: (info) => <span className="cell-number">{info.getValue()}</span>,
      }),
      column.display({
        id: 'actions',
        header: () => <span className="sr-only">Actions</span>,
        cell: ({ row }) => (
          <div className="cell-actions">
            {canEdit && (
              <button type="button" className="icon-button" onClick={() => startEdit(row.original)} aria-label={`Edit ${row.original.name}`} title="Edit inline (or double-click the row)">
                <PencilIcon size={16} />
              </button>
            )}
            {canDelete && (
              <button type="button" className="icon-button icon-button--danger" onClick={() => onDelete(row.original)} aria-label={`Delete ${row.original.name}`} title="Delete">
                <TrashIcon size={16} />
              </button>
            )}
          </div>
        ),
      }),
    ].filter((c) => c.id !== 'actions' || canEdit || canDelete),
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
  })

  return (
    <div className="grid-wrap" aria-busy={loading}>
      {loading && <div className="grid-progress" aria-hidden="true" />}
      <table className="grid grid--categories">
        <thead>
          {table.getHeaderGroups().map((group) => (
            <tr key={group.id}>
              {group.headers.map((header) => (
                <th
                  key={header.id}
                  className={`col-${header.column.id}`}
                  aria-sort={header.column.id !== 'actions' ? ariaSort(header.column.id === sortBy, sortDir) : undefined}
                >
                  {flexRender(header.column.columnDef.header, header.getContext())}
                </th>
              ))}
            </tr>
          ))}
        </thead>
        <tbody>
          {table.getRowModel().rows.map((row, index) =>
            edit?.category.id === row.original.id ? (
              <EditRow
                key={row.id}
                edit={edit}
                onChange={(draft) => setEdit({ ...edit, draft, errors: {} })}
                onSave={saveEdit}
                onCancel={cancelEdit}
              />
            ) : (
              <tr key={row.id} className="grid-row" style={staggerIndex(index)} onDoubleClick={() => startEdit(row.original)}>
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
  onChange: (draft: CategoryDraft) => void
  onSave: () => void
  onCancel: () => void
}

function EditRow({ edit, onChange, onSave, onCancel }: EditRowProps) {
  const { draft, errors, saving, category } = edit
  const firstInput = useRef<HTMLInputElement>(null)
  useEffect(() => firstInput.current?.focus(), [])

  const set = <K extends keyof CategoryDraft>(key: K, value: CategoryDraft[K]) => onChange({ ...draft, [key]: value })

  const onKeyDown = (e: KeyboardEvent<HTMLTableRowElement>) => {
    if (e.key === 'Escape') {
      e.preventDefault()
      onCancel()
    } else if (e.key === 'Enter' && !(e.target instanceof HTMLButtonElement)) {
      e.preventDefault()
      onSave()
    }
  }

  const invalid = (field: keyof CategoryErrors) => ({
    'aria-invalid': errors[field] ? true : undefined,
    title: errors[field],
    className: `input input--sm${errors[field] ? ' input--invalid' : ''}`,
  })

  return (
    <tr className="grid-row grid-row--editing" onKeyDown={onKeyDown} aria-label={`Editing ${category.name}`}>
      <td className="col-name">
        <div className="cell-category">
          <input
            type="color"
            className="color-input color-input--sm"
            value={draft.color}
            onChange={(e) => set('color', e.target.value)}
            aria-label="Color"
          />
          <input ref={firstInput} value={draft.name} onChange={(e) => set('name', e.target.value)} maxLength={50} aria-label="Name" {...invalid('name')} />
        </div>
      </td>
      <td className="col-budget">
        <input
          type="number"
          inputMode="decimal"
          min="0"
          step="0.01"
          value={draft.monthlyBudget}
          onChange={(e) => set('monthlyBudget', e.target.value)}
          placeholder="No limit"
          aria-label="Monthly budget"
          {...invalid('monthlyBudget')}
        />
      </td>
      <td className="col-expenses">
        <span className="cell-number">{category.expenseCount}</span>
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
