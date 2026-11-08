import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate, type Location } from 'react-router-dom'
import { ApiError } from '@/shared/api/httpClient'
import { useSession } from '@/shared/auth/useSession'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'

export interface LoginDraft {
  email: string
  password: string
}

type LoginErrors = Partial<Record<keyof LoginDraft, string>>

const EMAIL = /^[^\s@]+@[^\s@]+$/

function validate(draft: LoginDraft): LoginErrors {
  const errors: LoginErrors = {}
  if (!draft.email.trim()) errors.email = 'Enter your email address.'
  else if (!EMAIL.test(draft.email.trim())) errors.email = 'Enter a valid email address.'
  if (!draft.password) errors.password = 'Enter your password.'
  return errors
}

/** ViewModel for the login screen: form state, client-side checks, sign-in, and the return trip. */
export function useLoginViewModel() {
  const session = useSession()
  const navigate = useNavigate()
  const location = useLocation()
  const [draft, setDraft] = useState<LoginDraft>({ email: '', password: '' })
  const [errors, setErrors] = useState<LoginErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  // Where to go after signing in: the page that sent us here (never back to /login or /logout).
  const from = (location.state as { from?: Location } | null)?.from
  const returnTo = from && !['/login', '/logout'].includes(from.pathname) ? `${from.pathname}${from.search}` : '/'

  const set = (key: keyof LoginDraft, value: string) => {
    setDraft((d) => ({ ...d, [key]: value }))
    setErrors((e) => ({ ...e, [key]: undefined }))
    setFormError(null)
  }

  const submit = async (e?: FormEvent) => {
    e?.preventDefault()
    const clientErrors = validate(draft)
    setErrors(clientErrors)
    if (Object.keys(clientErrors).length > 0) return

    setSubmitting(true)
    setFormError(null)
    try {
      await session.signIn(draft.email.trim(), draft.password)
      navigate(returnTo, { replace: true })
    } catch (err) {
      const fieldErrors = fieldErrorsOf(err)
      if (fieldErrors) setErrors(fieldErrors)
      else setFormError(errorMessage(err))
      // Never keep a rejected password around.
      if (err instanceof ApiError && err.status === 401) setDraft((d) => ({ ...d, password: '' }))
    } finally {
      setSubmitting(false)
    }
  }

  return {
    status: session.status,
    returnTo,
    notice: session.endedBecause === 'expired' ? 'Your session ended. Please sign in again.' : null,
    draft,
    errors,
    formError,
    submitting,
    set,
    submit,
    /** Development convenience: fill in a seeded demo account. */
    fillDemoAccount: (email: string, password: string) => {
      setDraft({ email, password })
      setErrors({})
      setFormError(null)
    },
  }
}
