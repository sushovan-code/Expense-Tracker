import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Pagination } from './Pagination'

const setup = (page: number, totalPages = 3) => {
  const onPageChange = vi.fn()
  const onPageSizeChange = vi.fn()
  render(
    <Pagination
      page={page}
      pageSize={10}
      totalCount={25}
      totalPages={totalPages}
      onPageChange={onPageChange}
      onPageSizeChange={onPageSizeChange}
    />,
  )
  return { onPageChange, onPageSizeChange }
}

describe('Pagination', () => {
  it('shows the visible range and the page position', () => {
    setup(2)
    expect(screen.getByText('11–20 of 25')).toBeInTheDocument()
    expect(screen.getByText('2 / 3')).toBeInTheDocument()
  })

  it('disables backwards navigation on the first page and forwards on the last', () => {
    setup(1)
    expect(screen.getByRole('button', { name: 'Previous page' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Next page' })).toBeEnabled()
  })

  it('requests the next page and a new page size', async () => {
    const { onPageChange, onPageSizeChange } = setup(1)

    await userEvent.click(screen.getByRole('button', { name: 'Next page' }))
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Rows per page' }), '50')

    expect(onPageChange).toHaveBeenCalledWith(2)
    expect(onPageSizeChange).toHaveBeenCalledWith(50)
  })
})
