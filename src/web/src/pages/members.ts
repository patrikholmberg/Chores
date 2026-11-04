import type { Role } from '../auth/AuthContext'

/** A Member as returned by `/api/members`. */
export interface Member {
  id: string
  username: string
  displayName: string
  role: Role
  preferredLanguage: 'sv' | 'en'
  email: string | null
}
