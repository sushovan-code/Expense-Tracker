export interface ApiKey {
  id: string
  name: string
  /** Public part of the key, e.g. `etk_Ab3dE6gH`, to tell keys apart. */
  prefix: string
  /** Permissions the key is limited to; empty means all of the owner's. */
  scopes: string[]
  createdAt: string
  expiresAt: string
  lastUsedAt: string | null
  revokedAt: string | null
}

export interface CreatedApiKey {
  apiKey: ApiKey
  /** The full key. Returned once; the server only keeps its hash. */
  key: string
}

export interface ApiKeyInput {
  name: string
  scopes: string[]
  expiresInDays: number
}

export interface ChangePasswordInput {
  currentPassword: string
  newPassword: string
}

export const apiKeyStatus = (key: ApiKey, now = Date.now()): 'active' | 'revoked' | 'expired' =>
  key.revokedAt ? 'revoked' : Date.parse(key.expiresAt) <= now ? 'expired' : 'active'

export interface RegisterInput {
  email: string
  displayName: string
  password: string
}
