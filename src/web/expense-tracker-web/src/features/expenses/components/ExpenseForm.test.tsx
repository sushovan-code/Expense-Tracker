import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '@/test/renderWithProviders'
import { ExpenseForm } from './ExpenseForm'

const categories = [{ id: 'cat-food', name: 'Food', color: '#f97316' }]

describe('ExpenseForm', () => {
  it('saves an expense as private when the checkbox is ticked', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    renderWithProviders(<ExpenseForm categories={categories} onSubmit={onSubmit} onCancel={vi.fn()} />)

    await userEvent.type(screen.getByLabelText('Description'), 'Birthday gift')
    await userEvent.type(screen.getByLabelText('Amount'), '500')
    await userEvent.click(screen.getByLabelText(/Private/))
    await userEvent.click(screen.getByRole('button', { name: 'Add expense' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ description: 'Birthday gift', isPrivate: true })))
  })

  it('is shared with the family by default', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    renderWithProviders(<ExpenseForm categories={categories} onSubmit={onSubmit} onCancel={vi.fn()} />)

    await userEvent.type(screen.getByLabelText('Description'), 'Groceries')
    await userEvent.type(screen.getByLabelText('Amount'), '250')
    await userEvent.click(screen.getByRole('button', { name: 'Add expense' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ isPrivate: false })))
  })
})
