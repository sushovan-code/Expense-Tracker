import { SearchIcon } from '@/shared/ui/Icons'
import type { ExpensesViewModel } from '../viewModels/useExpensesViewModel'

/** Filter bar: search, date range, category and quick ranges. */
export function ExpenseFilters({ filters, hasFilters }: { filters: ExpensesViewModel['filters']; hasFilters: boolean }) {
  return (
    <div className="card filters">
      <label className="filters__search">
        <SearchIcon size={16} />
        <input
          className="input input--bare"
          type="search"
          placeholder="Search description or notes"
          value={filters.searchText}
          onChange={(e) => filters.setSearchText(e.target.value)}
          aria-label="Search expenses"
        />
      </label>
      <label className="filters__field">
        <span>From</span>
        <input className="input" type="date" value={filters.from} max={filters.to || undefined} onChange={(e) => filters.setFrom(e.target.value)} />
      </label>
      <label className="filters__field">
        <span>To</span>
        <input className="input" type="date" value={filters.to} min={filters.from || undefined} onChange={(e) => filters.setTo(e.target.value)} />
      </label>
      <label className="filters__field">
        <span>Category</span>
        <select className="input" value={filters.categoryId} onChange={(e) => filters.setCategory(e.target.value)}>
          <option value="">All categories</option>
          {filters.categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </label>
      <div className="filters__actions">
        <button type="button" className="button button--ghost" onClick={filters.showThisMonth}>
          This month
        </button>
        <button type="button" className="button button--ghost" onClick={filters.clear} disabled={!hasFilters}>
          Clear
        </button>
      </div>
    </div>
  )
}
