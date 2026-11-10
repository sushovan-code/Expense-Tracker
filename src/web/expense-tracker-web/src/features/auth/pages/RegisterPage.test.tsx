import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ApiError } from '@/shared/api/httpClient'
import { provide } from '@/shared/di/services'
import { renderWithProviders } from '@/test/renderWithProviders'
import { AccountApiToken, type AccountApi } from '../api/accountApi'
import { RegisterPage } from './RegisterPage'

const fakeAccountApi = (overrides: Partial<AccountApi> = {}): AccountApi => ({
  register: vi.fn().mockResolvedValue(undefined),
  changePassword: vi.fn(),
  listApiKeys: vi.fn(),
  createApiKey: vi.fn(),
  revokeApiKey: vi.fn(),
  ...overrides,
})

const renderPage = (api: AccountApi) =>
  renderWithProviders(<RegisterPage />, { user: null, route: '/register', services: [provide(AccountApiToken, api)] })

async function fillIn(email = 'meera@example.com', password = 'Password123') {
  await userEvent.type(screen.getByLabelText('Name'), 'Meera')
  await userEvent.type(screen.getByLabelText('Email'), email)
  await userEvent.type(screen.getByLabelText('Password'), password)
  await userEvent.click(screen.getByRole('button', { name: 'Create account' }))
}

describe('RegisterPage', () => {
  it('creates the account with the typed details', async () => {
    const api = fakeAccountApi()
    renderPage(api)

    await fillIn()

    await waitFor(() => expect(api.register).toHaveBeenCalledWith({ email: 'meera@example.com', displayName: 'Meera', password: 'Password123' }))
  })

  it('checks the password rules before calling the API', async () => {
    const api = fakeAccountApi()
    renderPage(api)

    await fillIn('meera@example.com', 'short')

    expect(await screen.findByText('Use at least 10 characters.')).toBeInTheDocument()
    expect(api.register).not.toHaveBeenCalled()
  })

  it('shows an email that is already registered next to the email field', async () => {
    const taken = new ApiError('An account with this email already exists.', 400, { email: ['An account with this email already exists.'] })
    renderPage(fakeAccountApi({ register: vi.fn().mockRejectedValue(taken) }))

    await fillIn()

    expect(await screen.findByText('An account with this email already exists.')).toBeInTheDocument()
  })

  it('links back to sign in', () => {
    renderPage(fakeAccountApi())

    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login')
  })
})
