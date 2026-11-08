import { Modal } from '@/shared/ui/Modal'
import { Pagination } from '@/shared/ui/grid/Pagination'
import { PlusIcon, SearchIcon } from '@/shared/ui/Icons'
import { EmptyState, ErrorState, LoadingState } from '@/shared/ui/States'
import { ROLE_ORDER } from '../viewModels/useAdminAccess'
import { useUserEditorViewModel } from '../viewModels/useUserEditorViewModel'
import { useUsersViewModel } from '../viewModels/useUsersViewModel'
import { CreateUserForm } from './CreateUserForm'
import { UserEditor } from './UserEditor'
import { UsersTable } from './UsersTable'

/** View: the user list with search, role filter, create and manage dialogs. */
export function UsersPanel() {
  const vm = useUsersViewModel()
  const editor = useUserEditorViewModel(vm.editor.userId, vm.editor.close)
  const { grid } = vm

  return (
    <>
      <div className="card filters filters--users">
        <label className="filters__search">
          <SearchIcon size={16} />
          <input
            className="input input--bare"
            type="search"
            placeholder="Search by name or email"
            value={vm.search.text}
            onChange={(e) => vm.search.setText(e.target.value)}
            aria-label="Search users"
          />
        </label>
        <select className="input" value={vm.roleFilter.value} onChange={(e) => vm.roleFilter.set(e.target.value)} aria-label="Filter by role">
          <option value="">All roles</option>
          {ROLE_ORDER.map((role) => (
            <option key={role} value={role}>
              {role}
            </option>
          ))}
        </select>
        {vm.createDialog.canOpen && (
          <button type="button" className="button button--primary" onClick={vm.createDialog.open}>
            <PlusIcon size={16} /> Add user
          </button>
        )}
      </div>

      <section className="card card--flush">
        {grid.error && !grid.data ? (
          <ErrorState error={grid.error} onRetry={grid.retry} />
        ) : grid.isLoading || !grid.data ? (
          <LoadingState />
        ) : grid.data.totalCount === 0 ? (
          <EmptyState title={vm.search.isActive ? 'No users match' : 'No users yet'} />
        ) : (
          <>
            <UsersTable
              data={grid.data}
              loading={grid.isRefreshing}
              sortBy={grid.sortBy}
              sortDir={grid.sortDir}
              onSort={grid.onSort}
              onOpen={grid.onOpen}
              currentUserId={vm.access.userId}
            />
            <Pagination
              page={grid.data.page}
              pageSize={grid.data.pageSize}
              totalCount={grid.data.totalCount}
              totalPages={grid.data.totalPages}
              pageSizeOptions={[10, 25, 50]}
              onPageChange={grid.onPageChange}
              onPageSizeChange={grid.onPageSizeChange}
              summary={<span className="muted">Select a user to manage their roles, claims and security</span>}
            />
          </>
        )}
      </section>

      <Modal open={vm.createDialog.isOpen} title="Add user" description="They can sign in right away with the password you set." onClose={vm.createDialog.close}>
        <CreateUserForm assignableRoles={vm.access.assignableRoles} onSubmit={vm.createDialog.submit} onCancel={vm.createDialog.close} />
      </Modal>

      <Modal
        open={vm.editor.userId !== null}
        size="lg"
        title={editor.user?.displayName ?? 'User'}
        description={editor.user?.email}
        onClose={vm.editor.close}
      >
        <UserEditor vm={editor} />
      </Modal>
    </>
  )
}
