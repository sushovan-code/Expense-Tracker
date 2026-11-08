import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { resolvePublicUrl } from '@/shared/config/env'
import { renderWithProviders } from '@/test/renderWithProviders'
import { SignInQrButton } from './SignInQrButton'

describe('SignInQrButton', () => {
  it('opens a QR code of the sign-in page at the configured address', async () => {
    renderWithProviders(<SignInQrButton publicUrl="http://192.168.1.20:5010" />, { user: null })

    await userEvent.click(screen.getByRole('button', { name: 'Open on your phone' }))

    expect(screen.getByRole('heading', { name: 'Open on your phone' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'QR code for http://192.168.1.20:5010/login' })).toBeInTheDocument()
    expect(screen.getByText('http://192.168.1.20:5010/login')).toBeInTheDocument()
  })
})

describe('resolvePublicUrl', () => {
  it('prefers the configured address and drops a trailing slash', () => {
    expect(resolvePublicUrl('https://expenses.example.test/', 'http://localhost:5010')).toBe('https://expenses.example.test')
  })

  it('falls back to the current origin when nothing is configured', () => {
    expect(resolvePublicUrl(undefined, 'http://localhost:5010')).toBe('http://localhost:5010')
    expect(resolvePublicUrl('  ', 'http://localhost:5010')).toBe('http://localhost:5010')
  })
})
