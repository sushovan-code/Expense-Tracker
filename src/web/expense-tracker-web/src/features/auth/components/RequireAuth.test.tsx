import { screen } from '@testing-library/react'
import { Route, Routes } from 'react-router-dom'
import { Permission, Role } from '@/shared/auth/permissions'
import type { SessionUser } from '@/shared/auth/session'
import { renderWithProviders } from '@/test/renderWithProviders'
import { RequireAuth } from './RequireAuth'

const reader: SessionUser = {
  id: 'r1',
  email: 'reader@example.com',
  displayName: 'Reader',
  roles: [Role.Reader],
  permissions: [Permission.ExpenseView],
}

function renderGuarded(user: SessionUser | null, guard: { permission?: Permission; minRole?: Role } = {}) {
  return renderWithProviders(
    <Routes>
      <Route path="/login" element={<p>Login page</p>} />
      <Route path="/secret" element={<RequireAuth {...guard}><p>Secret page</p></RequireAuth>} />
    </Routes>,
    { route: '/secret', user },
  )
}

describe('RequireAuth', () => {
  it('sends anonymous visitors to the login page', () => {
    renderGuarded(null)
    expect(screen.getByText('Login page')).toBeInTheDocument()
    expect(screen.queryByText('Secret page')).not.toBeInTheDocument()
  })

  it('shows the page to a user with the required permission', () => {
    renderGuarded(reader, { permission: Permission.ExpenseView })
    expect(screen.getByText('Secret page')).toBeInTheDocument()
  })

  it('explains instead of rendering when a permission is missing', () => {
    renderGuarded(reader, { permission: Permission.ExpenseAdd })
    expect(screen.getByRole('alert')).toHaveTextContent("You don't have access to this page")
    expect(screen.queryByText('Secret page')).not.toBeInTheDocument()
  })

  it('enforces "role or above"', () => {
    renderGuarded(reader, { minRole: Role.Admin })
    expect(screen.queryByText('Secret page')).not.toBeInTheDocument()

    renderGuarded({ ...reader, roles: [Role.SuperAdmin] }, { minRole: Role.Admin })
    expect(screen.getByText('Secret page')).toBeInTheDocument()
  })
})
