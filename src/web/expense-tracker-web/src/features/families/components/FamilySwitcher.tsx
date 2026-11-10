import { getActiveFamilyId, switchActiveFamily } from '@/shared/family/activeFamily'
import { useMyFamilies } from '../api/queries'

/**
 * Chooses which of my families I am working in. Shown only when I belong to two or more; the whole app
 * (expenses, categories, budgets, live updates) then follows the chosen family.
 */
export function FamilySwitcher() {
  const families = useMyFamilies().data ?? []
  if (families.length < 2) return null

  // A saved id I no longer belong to is ignored by the API, which uses the earliest joined family.
  const saved = getActiveFamilyId()
  const activeId = families.find((f) => f.id === saved)?.id ?? families[0].id

  return (
    <select
      className="input input--sm"
      aria-label="Active family"
      value={activeId}
      onChange={(e) => switchActiveFamily(e.target.value)}
    >
      {families.map((family) => (
        <option key={family.id} value={family.id}>
          {family.name}
        </option>
      ))}
    </select>
  )
}
