import type { User, UserManager } from 'oidc-client-ts'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { ApiError } from '../api/ApiError'
import { createApiFetch } from '../api/apiFetch'
import { AuthContext, type Auth, type AuthStatus, type CurrentMember } from './AuthContext'

/** Keeps the oidc-client-ts session and the signed-in Member's profile in React state. */
export function AuthProvider({ userManager, children }: { userManager: UserManager; children: ReactNode }) {
  const [user, setUser] = useState<User | null | undefined>(undefined)
  // The profile is stored with the subject it belongs to, so a different (or no) user never sees a stale one.
  const [profile, setProfile] = useState<{ subject: string; member?: CurrentMember; failed?: boolean }>()

  useEffect(() => {
    let active = true
    void userManager.getUser().then(async (stored) => {
      // An expired access token is fine as long as the refresh token can renew it.
      if (stored?.expired && stored.refresh_token) {
        stored = await userManager.signinSilent().catch(() => null)
      }
      if (active) setUser(stored && !stored.expired ? stored : null)
    })

    const loaded = (next: User) => setUser(next)
    const unloaded = () => setUser(null)
    const renewFailed = () => void userManager.removeUser()
    userManager.events.addUserLoaded(loaded)
    userManager.events.addUserUnloaded(unloaded)
    userManager.events.addSilentRenewError(renewFailed)
    return () => {
      active = false
      userManager.events.removeUserLoaded(loaded)
      userManager.events.removeUserUnloaded(unloaded)
      userManager.events.removeSilentRenewError(renewFailed)
    }
  }, [userManager])

  const api = useMemo(
    () =>
      createApiFetch({
        accessToken: async () => (await userManager.getUser())?.access_token,
        renew: async () => {
          const renewed = await userManager.signinSilent().catch(() => null)
          if (!renewed) await userManager.removeUser()
          return renewed !== null
        },
      }),
    [userManager],
  )

  const subject = user?.profile.sub
  const reloadMember = useCallback(async () => {
    if (subject) {
      const member = await api<CurrentMember>('/api/me')
      setProfile({ subject, member })
    }
  }, [api, subject])

  useEffect(() => {
    if (!subject) {
      return
    }
    api<CurrentMember>('/api/me').then(
      (member) => setProfile({ subject, member }),
      (error: unknown) => {
        // The Member is gone or the session is no longer accepted: forget it. Otherwise (offline) carry on without a profile.
        if (error instanceof ApiError && [401, 403, 404].includes(error.status)) {
          void userManager.removeUser()
        } else {
          setProfile({ subject, failed: true })
        }
      },
    )
  }, [subject, api, userManager])

  const current = profile && profile.subject === subject ? profile : undefined
  const member = current?.member
  const memberFailed = current?.failed ?? false

  const value = useMemo<Auth>(() => {
    const status: AuthStatus =
      user === undefined || (user && !member && !memberFailed) ? 'loading' : user ? 'signedIn' : 'signedOut'
    return {
      status,
      member,
      api,
      signIn: (returnTo = '/') => userManager.signinRedirect({ state: { returnTo } }),
      signOut: async () => {
        await userManager.revokeTokens(['refresh_token']).catch(() => undefined)
        await userManager.removeUser()
      },
      reloadMember,
    }
  }, [user, member, memberFailed, api, userManager, reloadMember])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
