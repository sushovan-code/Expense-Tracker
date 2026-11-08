import { staggerIndex } from '@/shared/lib/motion'
import { LockIcon } from '@/shared/ui/Icons'
import { ErrorState, LoadingState } from '@/shared/ui/States'
import { useRolesViewModel } from '../viewModels/useRolesViewModel'

/** View: each role's permission matrix. Editable only for roles below the admin's own rank. */
export function RolesPanel() {
  const vm = useRolesViewModel()

  if (vm.error && vm.isLoading) return <ErrorState error={vm.error} onRetry={vm.retry} />
  if (vm.isLoading) return <LoadingState />

  return (
    <div className="roles-grid stagger">
      {vm.cards.map((card, index) => (
        <section key={card.role.name} className="card role-card" style={staggerIndex(index)} aria-labelledby={`role-${card.role.name}`}>
          <header className="role-card__header">
            <div>
              <h2 id={`role-${card.role.name}`} className="role-card__name">
                <span className={`role-badge role-badge--${card.role.name.toLowerCase()}`}>{card.role.name}</span>
                {!card.editable && (
                  <span className="role-card__locked" title="You can't change this role">
                    <LockIcon size={14} />
                  </span>
                )}
              </h2>
              <p className="muted role-card__description">{card.role.description}</p>
            </div>
            <span className="muted role-card__count">
              {card.role.userCount} user{card.role.userCount === 1 ? '' : 's'}
            </span>
          </header>

          <div className="role-card__groups">
            {vm.groups.map((group) => (
              <fieldset key={group.name} className="permission-group">
                <legend>{group.name}</legend>
                {group.permissions.map((permission) => (
                  <label key={permission.name} className="choice choice--compact" title={permission.description}>
                    <input
                      type="checkbox"
                      checked={card.selected.includes(permission.name)}
                      disabled={!card.canToggle(permission.name)}
                      onChange={() => card.toggle(permission.name)}
                    />
                    <span>{permission.name.split('.')[1]}</span>
                  </label>
                ))}
              </fieldset>
            ))}
          </div>

          {card.editable && (
            <div className="form-actions">
              <button type="button" className="button button--ghost button--sm" onClick={card.reset} disabled={!card.dirty || card.saving}>
                Reset
              </button>
              <button type="button" className="button button--primary button--sm" onClick={() => void card.save()} disabled={!card.dirty || card.saving}>
                {card.saving ? 'Saving…' : 'Save permissions'}
              </button>
            </div>
          )}
        </section>
      ))}
    </div>
  )
}
