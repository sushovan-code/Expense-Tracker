import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { provide } from '@/shared/di/services'
import { switchActiveFamily } from '@/shared/family/activeFamily'
import { fakeFamiliesApi } from '@/test/fakeFamiliesApi'
import { renderWithProviders } from '@/test/renderWithProviders'
import { FamiliesApiToken } from '../api/familiesApi'
import { FamilySwitcher } from './FamilySwitcher'

vi.mock('@/shared/family/activeFamily', () => ({
  switchActiveFamily: vi.fn(),
  getActiveFamilyId: () => 'fam-2',
  FAMILY_HEADER: 'X-Family-Id',
}))

const two = [
  { id: 'fam-1', name: 'Me and my wife', role: 'owner' as const },
  { id: 'fam-2', name: 'My parents', role: 'owner' as const },
]

describe('FamilySwitcher', () => {
  it('is hidden when the person belongs to a single family', async () => {
    const api = fakeFamiliesApi({ listMine: vi.fn().mockResolvedValue([two[0]]) })
    renderWithProviders(<FamilySwitcher />, { services: [provide(FamiliesApiToken, api)] })

    await vi.waitFor(() => expect(api.listMine).toHaveBeenCalled())
    expect(screen.queryByRole('combobox', { name: 'Active family' })).not.toBeInTheDocument()
  })

  it('shows the saved family and switches to another', async () => {
    const api = fakeFamiliesApi({ listMine: vi.fn().mockResolvedValue(two) })
    renderWithProviders(<FamilySwitcher />, { services: [provide(FamiliesApiToken, api)] })

    const select = await screen.findByRole('combobox', { name: 'Active family' })
    expect(select).toHaveValue('fam-2')

    await userEvent.selectOptions(select, 'fam-1')

    expect(switchActiveFamily).toHaveBeenCalledWith('fam-1')
  })
})
