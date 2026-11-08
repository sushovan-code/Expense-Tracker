import { useCountUp } from '@/shared/hooks/useCountUp'
import { formatCurrency } from '@/shared/lib/format'

export function AnimatedCurrency({ value }: { value: number }) {
  const animated = useCountUp(value)
  return <span aria-label={formatCurrency(value)}>{formatCurrency(animated)}</span>
}

export function AnimatedNumber({ value }: { value: number }) {
  return <>{Math.round(useCountUp(value))}</>
}
