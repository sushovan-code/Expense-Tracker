import { createContext, useContext } from 'react'

/**
 * A tiny, typed dependency-injection container for React.
 *
 * Features declare *what* they need as tokens (`createServiceToken<ExpensesApi>('ExpensesApi')`) and
 * resolve it with `useService(token)`. Only the composition root (src/app) decides *which*
 * implementation backs each token — so features never construct their own dependencies, and tests
 * can provide fakes without mocking modules.
 */
export interface ServiceToken<T> {
  readonly key: symbol
  readonly name: string
  /** Phantom field that carries the service type; never set at runtime. */
  readonly __type?: T
}

export const createServiceToken = <T,>(name: string): ServiceToken<T> => ({ key: Symbol(name), name })

export type ServiceRegistration = readonly [ServiceToken<unknown>, unknown]

/** Pairs a token with its implementation (type-checked). */
export const provide = <T,>(token: ServiceToken<T>, implementation: T): ServiceRegistration =>
  [token as ServiceToken<unknown>, implementation]

export const ServicesContext = createContext<ReadonlyMap<symbol, unknown> | null>(null)

export function useService<T>(token: ServiceToken<T>): T {
  const registry = useContext(ServicesContext)
  if (!registry?.has(token.key)) {
    throw new Error(`Service "${token.name}" is not registered. Provide it in the composition root (src/app/services.ts).`)
  }
  return registry.get(token.key) as T
}
