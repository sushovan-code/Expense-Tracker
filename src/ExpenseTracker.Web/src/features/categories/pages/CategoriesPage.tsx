import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { Pagination } from '@/shared/ui/grid/Pagination'
import { PlusIcon, SearchIcon } from '@/shared/ui/Icons'
import { Modal } from '@/shared/ui/Modal'
import { EmptyState, ErrorState, LoadingState } from '@/shared/ui/States'
import { CategoriesGrid } from '../components/CategoriesGrid'
import { CategoryForm } from '../components/CategoryForm'
import { useCategoriesViewModel } from '../viewModels/useCategoriesViewModel'

/** View: binds the Categories ViewModel to presentational components. */
export function CategoriesPage() {
  const vm = useCategoriesViewModel()
  const { grid } = vm
  const deleting = vm.deleteDialog.target

  return (
    <>
      <div className="page-header">
        <div>
          <h1 className="page-title">Categories</h1>
          <p className="page-subtitle">Group your expenses and set monthly budgets</p>
        </div>
        {vm.can.add && (
          <button type="button" className="button button--primary" onClick={vm.createDialog.open}>
            <PlusIcon size={16} /> Add category
          </button>
        )}
      </div>

      <div className="card filters filters--compact">
        <label className="filters__search">
          <SearchIcon size={16} />
          <input
            className="input input--bare"
            type="search"
            placeholder="Search categories"
            value={vm.search.text}
            onChange={(e) => vm.search.setText(e.target.value)}
            aria-label="Search categories"
          />
        </label>
      </div>

      <section className="card card--flush">
        {grid.error && !grid.data ? (
          <ErrorState error={grid.error} onRetry={grid.retry} />
        ) : grid.isLoading || !grid.data ? (
          <LoadingState />
        ) : grid.data.totalCount === 0 ? (
          <EmptyState title={vm.search.isActive ? 'No categories match' : 'No categories yet'} />
        ) : (
          <>
            <CategoriesGrid
              data={grid.data}
              loading={grid.isRefreshing}
              sortBy={grid.sortBy}
              sortDir={grid.sortDir}
              onSort={grid.onSort}
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
              pageSizeOptions={[5, 10, 25, 50]}
              onPageChange={grid.onPageChange}
              onPageSizeChange={grid.onPageSizeChange}
              summary={vm.can.edit ? <span className="muted">Double-click a row to edit its name, color or budget</span> : undefined}
            />
          </>
        )}
      </section>

      <Modal
        open={vm.createDialog.isOpen}
        title="Add category"
        description="Pick a name, a color and (optionally) a monthly budget."
        onClose={vm.createDialog.close}
      >
        <CategoryForm onSubmit={vm.createDialog.submit} onCancel={vm.createDialog.close} />
      </Modal>

      <ConfirmDialog
        open={deleting !== null}
        title="Delete category?"
        message={
          <>
            The <strong>{deleting?.name}</strong> category will be removed.
            {deleting && deleting.expenseCount > 0 &&
              ` It still has ${deleting.expenseCount} expense${deleting.expenseCount === 1 ? '' : 's'}, so you'll need to move or delete them first.`}
          </>
        }
        onConfirm={vm.deleteDialog.confirm}
        onClose={vm.deleteDialog.cancel}
      />
    </>
  )
}
