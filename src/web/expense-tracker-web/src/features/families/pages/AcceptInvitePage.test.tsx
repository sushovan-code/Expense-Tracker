import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { provide } from '@/shared/di/services'
import { fakeFamiliesApi } from '@/test/fakeFamiliesApi'
import { renderWithProviders } from '@/test/renderWithProviders'
import { FamiliesApiToken } from '../api/familiesApi'
import { AcceptInvitePage } from './AcceptInvitePage'

describe('AcceptInvitePage', () => {
  it('creates an account from the invite token', async () => {
    const api = fakeFamiliesApi()
    renderWithProviders(<AcceptInvitePage />, { user: null, route: '/accept-invite?token=secret-token', services: [provide(FamiliesApiToken, api)] })

    await userEvent.type(screen.getByLabelText('Name'), 'Meera')
    await userEvent.type(screen.getByLabelText('Email'), 'meera@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'Password123')
    await userEvent.click(screen.getByRole('button', { name: 'Create account and join' }))

    await waitFor(() =>
      expect(api.acceptInvite).toHaveBeenCalledWith({
        token: 'secret-token', displayName: 'Meera', email: 'meera@example.com', password: 'Password123',
      }),
    )
  })

  it('lets a signed-in person join with their own account', async () => {
    const api = fakeFamiliesApi()
    renderWithProviders(<AcceptInvitePage />, { route: '/accept-invite?token=secret-token', services: [provide(FamiliesApiToken, api)] })

    await userEvent.click(screen.getByRole('button', { name: 'Join with this account' }))

    await waitFor(() => expect(api.join).toHaveBeenCalledWith('secret-token'))
  })

  it('explains an incomplete link', () => {
    renderWithProviders(<AcceptInvitePage />, { user: null, route: '/accept-invite', services: [provide(FamiliesApiToken, fakeFamiliesApi())] })

    expect(screen.getByRole('alert')).toHaveTextContent('incomplete')
  })
})
