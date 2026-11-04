import { useMemo, useRef, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { supportedLanguages } from '../i18n'
import { useAuth } from '../auth/AuthContext'
import { CaptchaField } from '../forms/CaptchaField'
import { describeError, minimumPasswordLength } from '../forms/errors'
import { SelectField } from '../forms/SelectField'
import { TextField } from '../forms/TextField'
import { deviceTimeZone, timeZones } from '../forms/timeZones'
import { useUsernameAvailability } from '../forms/useUsernameAvailability'

/** Open signup: creates a Family and its first Parent, then signs that Parent in. */
export function SignupPage() {
  const { t, i18n } = useTranslation()
  const { api, signIn } = useAuth()
  const form = useRef<HTMLFormElement>(null)
  const [values, setValues] = useState(() => ({
    familyName: '',
    timeZone: deviceTimeZone(),
    username: '',
    displayName: '',
    password: '',
    email: '',
    language: i18n.resolvedLanguage ?? 'en',
  }))
  const [error, setError] = useState<unknown>()
  const [busy, setBusy] = useState(false)
  const availability = useUsernameAvailability(values.username)
  const zones = useMemo(() => timeZones(values.timeZone), [values.timeZone])

  const set = (field: keyof typeof values) => (event: { target: { value: string } }) =>
    setValues((current) => ({ ...current, [field]: event.target.value }))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(undefined)
    const captcha = new FormData(form.current!).get('altcha')?.toString() ?? ''
    try {
      await api('/api/families', { method: 'POST', body: { ...values, captcha } })
      // Sets the sign-in cookie, so the authorization request that follows needs no second password prompt.
      await api('/api/account/login', { method: 'POST', body: { username: values.username, password: values.password } })
      await signIn('/')
    } catch (e) {
      setError(e)
      setBusy(false)
    }
  }

  const errors = describeError(t, error, { min: minimumPasswordLength.parent })
  const usernameError =
    errors.field('username') ??
    (availability === 'taken' ? t('errors.username.taken') : availability === 'invalid' ? t('errors.username.invalid') : undefined)

  return (
    <div className="row justify-content-center">
      <div className="col-md-9 col-lg-7">
        <h2 className="h3">{t('signup.title')}</h2>
        <p className="text-body-secondary">{t('signup.intro')}</p>
        {errors.form && (
          <div className="alert alert-danger" role="alert">
            {errors.form}
          </div>
        )}
        <form ref={form} onSubmit={submit} noValidate>
          <fieldset className="mb-2">
            <legend className="h5">{t('signup.familySection')}</legend>
            <TextField label={t('fields.familyName')} value={values.familyName} onChange={set('familyName')} error={errors.field('familyName')} required />
            <SelectField label={t('fields.timeZone')} value={values.timeZone} onChange={set('timeZone')} error={errors.field('timeZone')}>
              {zones.map((zone) => (
                <option key={zone} value={zone}>
                  {zone}
                </option>
              ))}
            </SelectField>
          </fieldset>
          <fieldset className="mb-2">
            <legend className="h5">{t('signup.parentSection')}</legend>
            <TextField
              label={t('fields.username')}
              autoComplete="username"
              autoCapitalize="none"
              spellCheck={false}
              value={values.username}
              onChange={set('username')}
              error={usernameError}
              className={availability === 'available' && !usernameError ? 'is-valid' : undefined}
              hint={availability === 'available' ? t('signup.usernameAvailable') : t('fields.usernameHint')}
              required
            />
            <TextField label={t('fields.displayName')} value={values.displayName} onChange={set('displayName')} error={errors.field('displayName')} hint={t('fields.displayNameHint')} />
            <TextField
              label={t('fields.password')}
              type="password"
              autoComplete="new-password"
              value={values.password}
              onChange={set('password')}
              error={errors.field('password')}
              hint={t('fields.passwordHint', { min: minimumPasswordLength.parent })}
              required
            />
            <TextField label={t('fields.email')} type="email" autoComplete="email" value={values.email} onChange={set('email')} error={errors.field('email')} hint={t('fields.emailHint')} required />
            <SelectField label={t('language.label')} value={values.language} onChange={set('language')} error={errors.field('language')}>
              {supportedLanguages.map((lng) => (
                <option key={lng} value={lng}>
                  {t(`language.${lng}`)}
                </option>
              ))}
            </SelectField>
          </fieldset>
          <CaptchaField error={errors.field('captcha')} />
          <button type="submit" className="btn btn-primary w-100" disabled={busy}>
            {t('signup.submit')}
          </button>
        </form>
        <p className="mt-4 text-center text-body-secondary">
          {t('signup.haveAccount')} <Link to="/login">{t('signup.signIn')}</Link>
        </p>
      </div>
    </div>
  )
}
