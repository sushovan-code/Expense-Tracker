import { formatTimestamp } from '@/shared/lib/format'
import { staggerIndex } from '@/shared/lib/motion'
import type { PagedResponse, SortDirection } from '@/shared/types'
import { SortHeader } from '@/shared/ui/grid/SortHeader'
import { ariaSort } from '@/shared/ui/grid/sorting'
import type { UserSortField, UserSummary } from '../model/types'
import { RoleBadges, UserStatus } from './badges'

interface UsersTableProps {
  data: PagedResponse<UserSummary>
  loading: boolean
  sortBy: UserSortField
  sortDir: SortDirection
  onSort: (field: UserSortField) => void
  onOpen: (id: string) => void
  /** Id of the signed-in administrator, marked as "you". */
  currentUserId: string | undefined
}

export function UsersTable({ data, loading, sortBy, sortDir, onSort, onOpen, currentUserId }: UsersTableProps) {
  const header = (label: string, field: UserSortField) => (
    <th className={`col-${field}`} aria-sort={ariaSort(sortBy === field, sortDir)}>
      <SortHeader label={label} field={field} sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
    </th>
  )

  return (
    <div className="grid-wrap" aria-busy={loading}>
      {loading && <div className="grid-progress" aria-hidden="true" />}
      <table className="grid grid--users">
        <thead>
          <tr>
            {header('User', 'name')}
            <th>Roles</th>
            <th>Status</th>
            {header('Last sign-in', 'lastlogin')}
            {header('Created', 'created')}
          </tr>
        </thead>
        <tbody>
          {data.items.map((user, index) => (
            <tr key={user.id} className="grid-row grid-row--clickable" style={staggerIndex(index)} onClick={() => onOpen(user.id)}>
              <td>
                <div className="cell-stack">
                  <button
                    type="button"
                    className="link-button cell-strong"
                    onClick={(e) => {
                      e.stopPropagation()
                      onOpen(user.id)
                    }}
                  >
                    {user.displayName}
                    {user.id === currentUserId && <span className="muted"> (you)</span>}
                  </button>
                  <span className="cell-muted">{user.email}</span>
                </div>
              </td>
              <td>
                <RoleBadges roles={user.roles} />
              </td>
              <td>
                <UserStatus user={user} />
              </td>
              <td className="cell-muted">{user.lastLoginAt ? formatTimestamp(user.lastLoginAt) : 'Never'}</td>
              <td className="cell-muted">{formatTimestamp(user.createdAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
