import { createContext, useContext } from 'react'
import type { ApiFetch } from '../api/apiFetch'

export type Role = 'parent' | 'child'

/** The signed-in Member's profile from `GET /api/me`. */
export interface CurrentMember {
  memberId: string
  username: string
  displayName: string
  role: Role
  preferredLanguage: 'sv' | 'en'
  familyId: string
  familyName: string
  timeZone: string
}

export type AuthStatus = 'loading' | 'signedOut' | 'signedIn'

export interface Auth {
  status: AuthStatus
  /** Loaded after sign-in; undefined while loading or signed out. */
  member: CurrentMember | undefined
  /** Calls the Api as the signed-in Member (or anonymously). */
  api: ApiFetch
  /** Starts the Authorization Code flow, coming back to `returnTo` afterwards. */
  signIn(returnTo?: string): Promise<void>
  /** Revokes the refresh token and forgets the session on this device. */
  signOut(): Promise<void>
  /** Reloads the profile, for example after changing the language. */
  reloadMember(): Promise<void>
}

export const AuthContext = createContext<Auth | undefined>(undefined)

export function useAuth(): Auth {
  const auth = useContext(AuthContext)
  if (!auth) {
    throw new Error('useAuth must be used inside an AuthProvider.')
  }
  return auth
}
