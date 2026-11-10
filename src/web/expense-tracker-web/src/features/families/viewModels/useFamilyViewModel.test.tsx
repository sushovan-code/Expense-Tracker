import { act, waitFor } from '@testing-library/react'
import { ApiError } from '@/shared/api/httpClient'
import { provide } from '@/shared/di/services'
import { fakeFamiliesApi, sharma } from '@/test/fakeFamiliesApi'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { FamiliesApiToken } from '../api/familiesApi'
import { inviteLink, useFamilyViewModel } from './useFamilyViewModel'

const render = (api = fakeFamiliesApi()) => ({
  api,
  ...renderHookWithProviders(() => useFamilyViewModel(), { services: [provide(FamiliesApiToken, api)] }),
})

describe('useFamilyViewModel', () => {
  it('treats a missing family as "no family yet", not as an error', async () => {
    const { result } = render(fakeFamiliesApi({ getMine: vi.fn().mockRejectedValue(new ApiError('Create or join a family first.', 403)) }))

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.family).toBeNull()
    expect(result.current.error).toBeNull()
  })

  it('creates a family', async () => {
    const { result, api } = render(fakeFamiliesApi({ getMine: vi.fn().mockResolvedValue(null) }))
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(() => result.current.createFamily('Sharma family'))

    expect(api.create).toHaveBeenCalledWith('Sharma family')
  })

  it('lets only owners manage the family', async () => {
    const owner = render()
    await waitFor(() => expect(owner.result.current.family).toBeTruthy())
    expect(owner.result.current.can.manage).toBe(true)

    const viewer = render(fakeFamiliesApi({ getMine: vi.fn().mockResolvedValue({ ...sharma, myRole: 'viewer' }) }))
    await waitFor(() => expect(viewer.result.current.family).toBeTruthy())
    expect(viewer.result.current.can.manage).toBe(false)
  })

  it('shows the invite link once it is created', async () => {
    const { result, api } = render()
    await waitFor(() => expect(result.current.family).toBeTruthy())

    await act(() => result.current.inviteDialog.create('member'))

    expect(api.createInvite).toHaveBeenCalledWith('member')
    expect(result.current.inviteDialog.created?.token).toBe('secret-token')

    act(() => result.current.inviteDialog.close())
    expect(result.current.inviteDialog.created).toBeNull()
  })

  it('changes a role and removes a member only after confirmation', async () => {
    const { result, api } = render()
    await waitFor(() => expect(result.current.family).toBeTruthy())
    const ravi = sharma.members[1]

    await act(() => result.current.changeRole(ravi, 'viewer'))
    expect(api.changeRole).toHaveBeenCalledWith('user-2', 'viewer')

    act(() => result.current.removeDialog.open(ravi))
    expect(api.removeMember).not.toHaveBeenCalled()
    await act(() => result.current.removeDialog.confirm())
    expect(api.removeMember).toHaveBeenCalledWith('user-2')
  })
})

describe('inviteLink', () => {
  it('points at the accept-invite page of this site', () => {
    expect(inviteLink('a b', 'https://expenses.example.test')).toBe('https://expenses.example.test/accept-invite?token=a%20b')
  })
})
