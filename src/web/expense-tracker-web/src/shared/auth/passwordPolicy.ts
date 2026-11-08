/** Mirrors the API's Identity password options, for instant feedback. The server remains the authority. */
export const PASSWORD_MIN_LENGTH = 10

export const PASSWORD_HINT = `At least ${PASSWORD_MIN_LENGTH} characters, with upper- and lowercase letters and a digit.`

/** The first rule the password breaks, or null when it satisfies the policy. */
export function passwordProblem(password: string): string | null {
  if (password.length < PASSWORD_MIN_LENGTH) return `Use at least ${PASSWORD_MIN_LENGTH} characters.`
  if (!/[a-z]/.test(password)) return 'Add a lowercase letter.'
  if (!/[A-Z]/.test(password)) return 'Add an uppercase letter.'
  if (!/\d/.test(password)) return 'Add a digit.'
  return null
}
