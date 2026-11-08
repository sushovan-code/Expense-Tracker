import { toCategoryInput, validateCategory } from './validation'

describe('validateCategory', () => {
  it('accepts a name, a hex color and an optional budget', () => {
    expect(validateCategory({ name: 'Travel', color: '#4f46e5', monthlyBudget: '' })).toEqual({})
    expect(validateCategory({ name: 'Travel', color: '#4F46E5', monthlyBudget: '1500' })).toEqual({})
  })

  it('rejects a blank name, an invalid color and a negative budget', () => {
    expect(validateCategory({ name: ' ', color: 'blue', monthlyBudget: '-1' })).toEqual({
      name: expect.any(String),
      color: expect.any(String),
      monthlyBudget: expect.any(String),
    })
  })
})

describe('toCategoryInput', () => {
  it('normalises the color and treats an empty budget as "no limit"', () => {
    expect(toCategoryInput({ name: ' Travel ', color: '#4F46E5', monthlyBudget: ' ' })).toEqual({
      name: 'Travel',
      color: '#4f46e5',
      monthlyBudget: null,
    })
  })
})
