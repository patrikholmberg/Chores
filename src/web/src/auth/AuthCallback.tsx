import type { UserManager } from 'oidc-client-ts'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { Loading } from '../shell/Loading'

// The code can only be redeemed once; StrictMode runs effects twice, so share the first attempt.
const pending = new Map<string, Promise<unknown>>()

/** Completes the Authorization Code flow at `/auth/callback` and returns to where sign-in started. */
export function AuthCallback({ userManager }: { userManager: UserManager }) {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    const url = window.location.href
    if (!pending.has(url)) {
      pending.set(url, userManager.signinCallback(url))
    }
    pending.get(url)!
      .then((user) => {
        const state = (user as { state?: { returnTo?: string } } | undefined)?.state
        navigate(state?.returnTo ?? '/', { replace: true })
      })
      .catch(() => setFailed(true))
  }, [userManager, navigate])

  if (failed) {
    return (
      <div className="alert alert-danger" role="alert">
        {t('auth.callbackFailed')} <Link to="/login">{t('auth.tryAgain')}</Link>
      </div>
    )
  }
  return <Loading />
}
