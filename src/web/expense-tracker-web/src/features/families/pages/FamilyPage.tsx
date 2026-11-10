import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { PlusIcon } from '@/shared/ui/Icons'
import { Modal } from '@/shared/ui/Modal'
import { EmptyState, ErrorState, LoadingState } from '@/shared/ui/States'
import { AddMemberForm } from '../components/AddMemberForm'
import { FamilyNameForm } from '../components/FamilyNameForm'
import { InvitePanel } from '../components/InvitePanel'
import { MembersTable } from '../components/MembersTable'
import { useFamilyViewModel } from '../viewModels/useFamilyViewModel'

/** View: the family, its members and invites. Binds the ViewModel to presentational components. */
export function FamilyPage() {
  const vm = useFamilyViewModel()

  if (vm.error && vm.family === undefined) return <ErrorState error={vm.error} onRetry={vm.retry} />
  if (vm.isLoading) return <LoadingState />

  if (vm.family === null || vm.family === undefined) {
    return (
      <>
        <div className="page-header">
          <div>
            <h1 className="page-title">Family</h1>
            <p className="page-subtitle">Share expenses and budgets with the people you live with</p>
          </div>
        </div>
        <section className="card">
          <EmptyState title="You are not in a family yet">
            <p className="muted">Create one to start recording expenses, or ask a family owner for an invite link.</p>
            <FamilyNameForm submitLabel="Create family" onSubmit={vm.createFamily} />
          </EmptyState>
        </section>
      </>
    )
  }

  const { family } = vm
  return (
    <>
      <div className="page-header">
        <div>
          <h1 className="page-title">{family.name}</h1>
          <p className="page-subtitle">
            {family.members.length} {family.members.length === 1 ? 'member' : 'members'}
          </p>
        </div>
        {vm.can.manage && (
          <div className="page-actions">
            <button type="button" className="button button--ghost" onClick={vm.inviteDialog.open}>
              Invite link
            </button>
            <button type="button" className="button button--primary" onClick={vm.addDialog.open}>
              <PlusIcon size={16} /> Add member
            </button>
          </div>
        )}
      </div>

      <section className="card card--flush">
        <MembersTable
          members={family.members}
          currentUserId={vm.user?.id}
          canManage={vm.can.manage}
          onChangeRole={(member, role) => void vm.changeRole(member, role)}
          onRemove={vm.removeDialog.open}
          onLeave={vm.leaveDialog.open}
        />
      </section>

      {vm.can.manage && (
        <section className="card">
          <h2 className="card__title">Rename family</h2>
          <FamilyNameForm initialName={family.name} submitLabel="Save name" onSubmit={vm.rename} />
        </section>
      )}

      <Modal open={vm.addDialog.isOpen} title="Add member" description="Creates an account for them and adds it to your family." onClose={vm.addDialog.close}>
        <AddMemberForm onSubmit={vm.addDialog.submit} onCancel={vm.addDialog.close} />
      </Modal>

      <Modal open={vm.inviteDialog.isOpen} title="Invite link" description="Share the link yourself, for example in a message." onClose={vm.inviteDialog.close}>
        <InvitePanel
          created={vm.inviteDialog.created}
          openInvites={family.invites}
          onCreate={vm.inviteDialog.create}
          onRevoke={vm.revokeInvite}
          onDone={vm.inviteDialog.close}
        />
      </Modal>

      <ConfirmDialog
        open={vm.removeDialog.target !== null}
        title="Remove member?"
        message={
          <>
            <strong>{vm.removeDialog.target?.displayName}</strong> will lose access to this family&apos;s expenses. Their account stays.
          </>
        }
        confirmLabel="Remove"
        busyLabel="Removing…"
        onConfirm={vm.removeDialog.confirm}
        onClose={vm.removeDialog.cancel}
      />

      <ConfirmDialog
        open={vm.leaveDialog.isOpen}
        title="Leave this family?"
        message="You will no longer see its expenses. Someone must invite you to come back."
        confirmLabel="Leave"
        busyLabel="Leaving…"
        onConfirm={vm.leaveDialog.confirm}
        onClose={vm.leaveDialog.cancel}
      />
    </>
  )
}
