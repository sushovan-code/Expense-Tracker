import { useState, type FormEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { switchActiveFamily } from '@/shared/family/activeFamily'
import { PASSWORD_HINT, passwordProblem } from '@/shared/auth/passwordPolicy'
import { useSession } from '@/shared/auth/useSession'
import { errorMessage } from '@/shared/lib/errors'
import { useToast } from '@/shared/toast/toast'
import { useFamilyMutations } from '../api/queries'

const EMAIL = /^[^\s@]+@[^\s@]+$/

/**
 * ViewModel for the accept-invite page. Someone who is signed in without a family joins with their account;
 * a visitor creates an account and then signs in through the normal login.
 */
export function useAcceptInviteViewModel() {
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''
  const { status, user } = useSession()
  const mutations = useFamilyMutations()
  const navigate = useNavigate()
  const toast = useToast()

  const [draft, setDraft] = useState({ displayName: '', email: '', password: '' })
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState<string>()

  const set = (key: keyof typeof draft, value: string) => {
    setDraft((d) => ({ ...d, [key]: value }))
    setErrors((e) => ({ ...e, [key]: '' }))
  }

  const join = async () => {
    setFormError(undefined)
    try {
      const family = await mutations.join.mutateAsync(token)
      toast.success('Welcome to the family', { description: family.name })
      switchActiveFamily(family.id, '/family')
    } catch (err) {
      setFormError(errorMessage(err))
    }
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
    try {
      await mutations.acceptInvite.mutateAsync({ token, ...draft, email: draft.email.trim(), displayName: draft.displayName.trim() })
      toast.success('Account created', { description: 'Sign in to see your family.' })
      navigate('/login', { replace: true })
    } catch (err) {
      setFormError(errorMessage(err))
    }
  }

  return {
    hasToken: token.length > 0,
    isRestoring: status === 'restoring',
    isSignedIn: status === 'authenticated',
    signedInAs: user?.email,
    draft,
    set,
    errors,
    formError,
    passwordHint: PASSWORD_HINT,
    busy: mutations.join.isPending || mutations.acceptInvite.isPending,
    join,
    submit,
  }
}
