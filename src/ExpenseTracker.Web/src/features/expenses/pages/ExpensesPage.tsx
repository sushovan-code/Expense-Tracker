import { formatCurrency } from '@/shared/lib/format'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { Pagination } from '@/shared/ui/grid/Pagination'
import { DownloadIcon, PlusIcon } from '@/shared/ui/Icons'
import { Modal } from '@/shared/ui/Modal'
import { EmptyState, ErrorState, LoadingState } from '@/shared/ui/States'
import { BulkActionsBar } from '../components/BulkActionsBar'
import { ExpenseFilters } from '../components/ExpenseFilters'
import { ExpenseForm } from '../components/ExpenseForm'
import { ExpensesGrid } from '../components/ExpensesGrid'
import { useExpensesViewModel } from '../viewModels/useExpensesViewModel'

/** View: binds the Expenses ViewModel to presentational components. No data access or business logic. */
export function ExpensesPage() {
  const vm = useExpensesViewModel()
  const { summary, grid } = vm

  return (
    <>
      <div className="page-header">
        <div>
          <h1 className="page-title">Expenses</h1>
          <p className="page-subtitle">
            {summary.totalCount !== undefined
              ? `${summary.totalCount} ${summary.totalCount === 1 ? 'expense' : 'expenses'}${summary.hasFilters ? ' match' : ''} · ${formatCurrency(summary.totalAmount ?? 0)}`
              : ' '}
          </p>
        </div>
        <div className="page-actions">
          {vm.can.export && (
            <button type="button" className="button button--ghost" onClick={vm.exporting.run} disabled={vm.exporting.isBusy || !vm.exporting.canExport}>
              <DownloadIcon size={16} /> {vm.exporting.isBusy ? 'Exporting…' : 'Export CSV'}
            </button>
          )}
          {vm.can.add && (
            <button type="button" className="button button--primary" onClick={vm.createDialog.open} disabled={!vm.createDialog.canOpen}>
              <PlusIcon size={16} /> Add expense
            </button>
          )}
        </div>
      </div>

      <ExpenseFilters filters={vm.filters} hasFilters={summary.hasFilters} />
      {vm.can.delete && <BulkActionsBar selection={vm.selection} />}

      <section className="card card--flush">
        {grid.error && !grid.data ? (
          <ErrorState error={grid.error} onRetry={grid.retry} />
        ) : grid.isLoading || !grid.data || !grid.categories ? (
          <LoadingState />
        ) : grid.data.totalCount === 0 ? (
          <EmptyState title={summary.hasFilters ? 'No expenses match these filters' : 'No expenses yet'}>
            <p className="muted">{summary.hasFilters ? 'Try a different date range or clear the filters.' : vm.can.add ? 'Add your first expense to get started.' : 'Nothing has been recorded yet.'}</p>
          </EmptyState>
        ) : (
          <>
            <ExpensesGrid
              data={grid.data}
              loading={grid.isRefreshing}
              categories={grid.categories}
              sortBy={grid.sortBy}
              sortDir={grid.sortDir}
              onSort={grid.onSort}
              rowSelection={grid.rowSelection}
              onRowSelectionChange={grid.onRowSelectionChange}
              onSave={grid.onSaveRow}
              onDelete={grid.onDeleteRow}
              canEdit={vm.can.edit}
              canDelete={vm.can.delete}
            />
            <Pagination
              page={grid.data.page}
              pageSize={grid.data.pageSize}
              totalCount={grid.data.totalCount}
              totalPages={grid.data.totalPages}
              onPageChange={grid.onPageChange}
              onPageSizeChange={grid.onPageSizeChange}
              summary={
                <>
                  Total <strong>{formatCurrency(grid.data.totalAmount)}</strong>
                  {vm.can.edit && <span className="muted"> · double-click a row to edit</span>}
                </>
              }
            />
          </>
        )}
      </section>

      <Modal open={vm.createDialog.isOpen} title="Add expense" description="Record something you spent money on." onClose={vm.createDialog.close}>
        <ExpenseForm categories={vm.filters.categories} onSubmit={vm.createDialog.submit} onCancel={vm.createDialog.close} />
      </Modal>

      <ConfirmDialog
        open={vm.deleteDialog.target !== null}
        title="Delete expense?"
        message={
          <>
            <strong>{vm.deleteDialog.target?.description}</strong> ({formatCurrency(vm.deleteDialog.target?.amount ?? 0)}) will be removed.
            You can undo this from the notification.
          </>
        }
        onConfirm={vm.deleteDialog.confirm}
        onClose={vm.deleteDialog.cancel}
      />

      <ConfirmDialog
        open={vm.bulkDeleteDialog.isOpen}
        title={`Delete ${vm.bulkDeleteDialog.count} expense${vm.bulkDeleteDialog.count === 1 ? '' : 's'}?`}
        message={
          <>
            The <strong>{vm.bulkDeleteDialog.count} selected</strong> expense{vm.bulkDeleteDialog.count === 1 ? '' : 's'} will be removed. You
            can undo this from the notification.
          </>
        }
        confirmLabel={`Delete ${vm.bulkDeleteDialog.count}`}
        onConfirm={vm.bulkDeleteDialog.confirm}
        onClose={vm.bulkDeleteDialog.cancel}
      />
    </>
  )
}
