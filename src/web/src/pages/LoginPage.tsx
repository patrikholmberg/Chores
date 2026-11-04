import { useEffect, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Navigate, useSearchParams } from 'react-router'
import { useAuth } from '../auth/AuthContext'
import { describeError } from '../forms/errors'
import { TextField } from '../forms/TextField'
import { Loading } from '../shell/Loading'
import { redirectTo } from '../shell/redirect'

/** Only the authorize endpoint may be continued to, so the screen cannot be used as an open redirect. */
function continueUrl(returnUrl: string | null) {
  return returnUrl?.startsWith('/connect/authorize?') ? returnUrl : undefined
}

/**
 * The sign-in screen the authorization server sends the browser to. It checks the password, which sets a short-lived
 * cookie, and then continues the authorization request. Opened directly, it first starts that request.
 */
export function LoginPage() {
  const { t } = useTranslation()
  const { status, api, signIn } = useAuth()
  const [params] = useSearchParams()
  const next = continueUrl(params.get('returnUrl'))
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<unknown>()
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!next && status === 'signedOut') {
      void signIn('/')
    }
  }, [next, status, signIn])

  if (!next) {
    return status === 'signedIn' ? <Navigate to="/" replace /> : <Loading />
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(undefined)
    try {
      await api('/api/account/login', { method: 'POST', body: { username, password } })
      redirectTo(next)
    } catch (e) {
      setError(e)
      setBusy(false)
    }
  }

  const failure = error === undefined ? undefined : describeError(t, error).form

  return (
    <div className="row justify-content-center">
      <div className="col-sm-10 col-md-7 col-lg-5">
        <h2 className="h3 mb-3">{t('login.title')}</h2>
        {failure && (
          <div className="alert alert-danger" role="alert">
            {failure}
          </div>
        )}
        <form onSubmit={submit} noValidate>
          <TextField
            label={t('fields.username')}
            autoComplete="username"
            autoCapitalize="none"
            spellCheck={false}
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
          />
          <TextField
            label={t('fields.password')}
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
          <button type="submit" className="btn btn-primary w-100" disabled={busy || !username || !password}>
            {t('login.submit')}
          </button>
        </form>
        <p className="mt-4 text-center text-body-secondary">
          {t('login.noFamily')} <Link to="/signup">{t('login.createFamily')}</Link>
        </p>
      </div>
    </div>
  )
}
