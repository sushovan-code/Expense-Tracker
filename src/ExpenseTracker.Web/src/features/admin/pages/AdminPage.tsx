import { useUrlState } from '@/shared/hooks/useUrlState'
import { ShieldIcon, UsersIcon } from '@/shared/ui/Icons'
import { RolesPanel } from '../components/RolesPanel'
import { UsersPanel } from '../components/UsersPanel'
import { useAdminAccess } from '../viewModels/useAdminAccess'

const TAB_DEFAULTS = { tab: 'users' }

/** View: administration (Admin or above), with Users and Roles tabs. */
export function AdminPage() {
  const access = useAdminAccess()
  const [url, setUrl] = useUrlState(TAB_DEFAULTS)
  const tabs = [
    { id: 'users', label: 'Users', icon: <UsersIcon size={16} />, visible: access.canViewUsers },
    { id: 'roles', label: 'Roles & permissions', icon: <ShieldIcon size={16} />, visible: access.canViewRoles },
  ].filter((t) => t.visible)
  const active = tabs.find((t) => t.id === url.tab) ?? tabs[0]

  return (
    <>
      <div className="page-header">
        <div>
          <h1 className="page-title">Administration</h1>
          <p className="page-subtitle">Users, roles and what each role is allowed to do</p>
        </div>
        <div className="tabs glass" role="tablist" aria-label="Administration sections">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              role="tab"
              id={`tab-${tab.id}`}
              aria-selected={tab.id === active?.id}
              aria-controls={`panel-${tab.id}`}
              className="tabs__tab"
              onClick={() => setUrl({ tab: tab.id })}
            >
              {tab.icon} {tab.label}
            </button>
          ))}
        </div>
      </div>

      {active && (
        <div role="tabpanel" id={`panel-${active.id}`} aria-labelledby={`tab-${active.id}`}>
          {active.id === 'users' ? <UsersPanel /> : <RolesPanel />}
        </div>
      )}
    </>
  )
}
