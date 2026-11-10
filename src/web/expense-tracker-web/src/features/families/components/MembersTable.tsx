import { formatTimestamp } from '@/shared/lib/format'
import { staggerIndex } from '@/shared/lib/motion'
import { FAMILY_ROLES, FAMILY_ROLE_LABEL, type FamilyMember, type FamilyRole } from '../model/types'

interface MembersTableProps {
  members: FamilyMember[]
  currentUserId: string | undefined
  /** Owners can change roles and remove people. */
  canManage: boolean
  onChangeRole: (member: FamilyMember, role: FamilyRole) => void
  onRemove: (member: FamilyMember) => void
  onLeave: () => void
}

export function MembersTable({ members, currentUserId, canManage, onChangeRole, onRemove, onLeave }: MembersTableProps) {
  return (
    <div className="grid-wrap">
      <table className="grid">
        <thead>
          <tr>
            <th>Member</th>
            <th>Role</th>
            <th>Joined</th>
            <th aria-label="Actions" />
          </tr>
        </thead>
        <tbody>
          {members.map((member, index) => {
            const isMe = member.userId === currentUserId
            return (
              <tr key={member.userId} className="grid-row" style={staggerIndex(index)}>
                <td>
                  <div className="cell-stack">
                    <span className="cell-strong">
                      {member.displayName}
                      {isMe && <span className="muted"> (you)</span>}
                    </span>
                    <span className="cell-muted">{member.email}</span>
                  </div>
                </td>
                <td>
                  {canManage ? (
                    <select
                      className="input"
                      value={member.role}
                      aria-label={`Role of ${member.displayName}`}
                      onChange={(e) => onChangeRole(member, e.target.value as FamilyRole)}
                    >
                      {FAMILY_ROLES.map((role) => (
                        <option key={role} value={role}>
                          {FAMILY_ROLE_LABEL[role]}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <span className="role-badge">{FAMILY_ROLE_LABEL[member.role]}</span>
                  )}
                </td>
                <td className="cell-muted">{formatTimestamp(member.joinedAt)}</td>
                <td>
                  {isMe ? (
                    <button type="button" className="button button--ghost button--sm" onClick={onLeave}>
                      Leave
                    </button>
                  ) : (
                    canManage && (
                      <button type="button" className="button button--ghost button--sm" onClick={() => onRemove(member)}>
                        Remove
                      </button>
                    )
                  )}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
