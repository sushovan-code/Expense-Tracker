import { toExpenseInput, validateExpense, type ExpenseDraft } from './validation'

const valid: ExpenseDraft = { description: 'Groceries', amount: '250.50', date: '2026-10-05', categoryId: 1, notes: '' }

describe('validateExpense', () => {
  it('accepts a complete draft', () => {
    expect(validateExpense(valid)).toEqual({})
  })

  it.each([
    [{ description: '   ' }, 'description'],
    [{ amount: '' }, 'amount'],
    [{ amount: '0' }, 'amount'],
    [{ amount: '-3' }, 'amount'],
    [{ amount: '10.005' }, 'amount'],
    [{ date: '' }, 'date'],
    [{ categoryId: 0 }, 'categoryId'],
    [{ notes: 'x'.repeat(1001) }, 'notes'],
  ])('flags %o on %s', (patch, field) => {
    expect(validateExpense({ ...valid, ...patch })).toHaveProperty(field)
  })
})

describe('toExpenseInput', () => {
  it('trims text, parses the amount and nulls empty notes', () => {
    expect(toExpenseInput({ ...valid, description: '  Groceries ', notes: '   ' })).toEqual({
      description: 'Groceries',
      amount: 250.5,
      date: '2026-10-05',
      categoryId: 1,
      notes: null,
    })
  })
})
