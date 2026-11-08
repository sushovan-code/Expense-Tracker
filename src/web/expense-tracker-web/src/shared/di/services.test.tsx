import { renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'
import { createServiceToken, provide, useService } from './services'
import { ServicesProvider } from './ServicesProvider'

interface Greeter {
  greet(name: string): string
}
const GreeterToken = createServiceToken<Greeter>('Greeter')

describe('service container', () => {
  it('resolves the registered implementation', () => {
    const wrapper = ({ children }: { children: ReactNode }) => (
      <ServicesProvider services={[provide(GreeterToken, { greet: (n) => `Hi ${n}` })]}>{children}</ServicesProvider>
    )

    const { result } = renderHook(() => useService(GreeterToken), { wrapper })

    expect(result.current.greet('Ada')).toBe('Hi Ada')
  })

  it('lets a nested provider override a registration', () => {
    const wrapper = ({ children }: { children: ReactNode }) => (
      <ServicesProvider services={[provide(GreeterToken, { greet: () => 'outer' })]}>
        <ServicesProvider services={[provide(GreeterToken, { greet: () => 'inner' })]}>{children}</ServicesProvider>
      </ServicesProvider>
    )

    const { result } = renderHook(() => useService(GreeterToken), { wrapper })

    expect(result.current.greet('x')).toBe('inner')
  })

  it('fails fast with a helpful message when a service is missing', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    expect(() => renderHook(() => useService(GreeterToken))).toThrow(/Service "Greeter" is not registered/)
  })
})
