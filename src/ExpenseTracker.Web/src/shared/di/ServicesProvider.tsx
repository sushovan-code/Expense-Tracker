import { useContext, useMemo, type ReactNode } from 'react'
import { ServicesContext, type ServiceRegistration } from './services'

/** Registers service implementations for the subtree; nested providers inherit and can override. */
export function ServicesProvider({ services, children }: { services: readonly ServiceRegistration[]; children: ReactNode }) {
  const parent = useContext(ServicesContext)
  // Nested providers inherit and can override their parent's registrations.
  const registry = useMemo(() => {
    const map = new Map(parent ?? [])
    for (const [token, implementation] of services) map.set(token.key, implementation)
    return map
  }, [parent, services])

  return <ServicesContext.Provider value={registry}>{children}</ServicesContext.Provider>
}
