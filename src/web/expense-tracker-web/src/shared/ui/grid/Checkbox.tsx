import { useEffect, useRef, type InputHTMLAttributes } from 'react'

/** Checkbox with tri-state support (for "some rows selected"). */
export function Checkbox({ indeterminate = false, ...props }: InputHTMLAttributes<HTMLInputElement> & { indeterminate?: boolean }) {
  const ref = useRef<HTMLInputElement>(null)
  useEffect(() => {
    if (ref.current) ref.current.indeterminate = indeterminate && !props.checked
  }, [indeterminate, props.checked])
  return <input ref={ref} type="checkbox" className="checkbox" {...props} />
}
