import { useState } from 'react'
import { formatTimestamp } from '@/shared/lib/format'
import { useToast } from '@/shared/toast/toast'
import { CopyIcon } from '@/shared/ui/Icons'
import { FAMILY_ROLES, FAMILY_ROLE_LABEL, type CreatedInvite, type FamilyInvite, type FamilyRole } from '../model/types'
import { inviteLink } from '../viewModels/useFamilyViewModel'

interface InvitePanelProps {
  created: CreatedInvite | null
  openInvites: FamilyInvite[]
  onCreate: (role: FamilyRole) => Promise<void>
  onRevoke: (id: string) => Promise<void>
  onDone: () => void
}

/** Creates an invite link to share yourself (shown once) and lists the open ones. */
export function InvitePanel({ created, openInvites, onCreate, onRevoke, onDone }: InvitePanelProps) {
  const [role, setRole] = useState<FamilyRole>('member')
  const toast = useToast()
  const link = created ? inviteLink(created.token) : ''

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(link)
      toast.success('Link copied')
    } catch {
      toast.warning("Couldn't copy", { description: 'Select the link and copy it yourself.' })
    }
  }

  return (
    <div className="form">
      {created ? (
        <>
          <p className="muted">
            Share this link with the person. It works once and expires on {formatTimestamp(created.expiresAt)}. You will not see it again.
          </p>
          <div className="form-row">
            <input className="input" readOnly value={link} aria-label="Invite link" onFocus={(e) => e.currentTarget.select()} />
            <button type="button" className="button button--primary" onClick={() => void copy()}>
              <CopyIcon size={16} /> Copy
            </button>
          </div>
        </>
      ) : (
        <div className="form-row">
          <select className="input" value={role} aria-label="Role for the invited person" onChange={(e) => setRole(e.target.value as FamilyRole)}>
            {FAMILY_ROLES.map((r) => (
              <option key={r} value={r}>
                {FAMILY_ROLE_LABEL[r]}
              </option>
            ))}
          </select>
          <button type="button" className="button button--primary" onClick={() => void onCreate(role)}>
            Create invite link
          </button>
        </div>
      )}

      {openInvites.length > 0 && (
        <>
          <p className="field__label">Open invites</p>
          <div className="form">
            {openInvites.map((invite) => (
              <div key={invite.id} className="form-row">
                <span>
                  {FAMILY_ROLE_LABEL[invite.role]} <span className="muted">· expires {formatTimestamp(invite.expiresAt)}</span>
                </span>
                <button type="button" className="button button--ghost button--sm" onClick={() => void onRevoke(invite.id)}>
                  Revoke
                </button>
              </div>
            ))}
          </div>
        </>
      )}

      <div className="form-actions">
        <button type="button" className="button button--ghost" onClick={onDone}>
          Done
        </button>
      </div>
    </div>
  )
}
