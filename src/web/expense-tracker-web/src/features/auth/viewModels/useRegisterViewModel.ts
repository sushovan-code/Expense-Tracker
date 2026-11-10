import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { PASSWORD_HINT, passwordProblem } from '@/shared/auth/passwordPolicy'
import { useService } from '@/shared/di/services'
import { errorMessage, fieldErrorsOf } from '@/shared/lib/errors'
import { logger } from '@/shared/telemetry'
import { useToast } from '@/shared/toast/toast'
import { AccountApiToken } from '../api/accountApi'

const EMAIL = /^[^\s@]+@[^\s@]+$/

/** ViewModel for the register screen: validates the form, creates the account, then sends the person to sign in. */
export function useRegisterViewModel() {
  const api = useService(AccountApiToken)
  const navigate = useNavigate()
  const toast = useToast()

  const [draft, setDraft] = useState({ displayName: '', email: '', password: '' })
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState<string>()
  const [submitting, setSubmitting] = useState(false)

  const set = (key: keyof typeof draft, value: string) => {
    setDraft((d) => ({ ...d, [key]: value }))
    setErrors((e) => ({ ...e, [key]: '' }))
  }

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    const next: Record<string, string> = {}
    if (!draft.displayName.trim()) next.displayName = 'Enter your name.'
    if (!EMAIL.test(draft.email.trim())) next.email = 'Enter a valid email address.'
    const problem = passwordProblem(draft.password)
    if (problem) next.password = problem
    setErrors(next)
    if (Object.keys(next).length > 0) return

    setFormError(undefined)
    setSubmitting(true)
    try {
      await api.register({ email: draft.email.trim(), displayName: draft.displayName.trim(), password: draft.password })
      logger.event('auth.registered', 'A new account was registered')
      toast.success('Account created', { description: 'Sign in to create your family.' })
      navigate('/login', { replace: true })
    } catch (err) {
      const fieldErrors = fieldErrorsOf(err)
      if (fieldErrors) setErrors(fieldErrors)
      else setFormError(errorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return { draft, set, errors, formError, submitting, submit, passwordHint: PASSWORD_HINT }
}
