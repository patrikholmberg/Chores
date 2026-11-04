import { useEffect, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router'
import { useAuth, type Role } from '../auth/AuthContext'
import { describeError, minimumPasswordLength } from '../forms/errors'
import { SelectField } from '../forms/SelectField'
import { TextField } from '../forms/TextField'
import { useUsernameAvailability } from '../forms/useUsernameAvailability'
import { supportedLanguages } from '../i18n'
import { ConfirmDialog } from '../shell/ConfirmDialog'
import { Loading } from '../shell/Loading'
import { useBootstrapModal } from '../shell/useBootstrapModal'
import type { Member } from './members'

interface FormValues {
  username: string
  displayName: string
  role: Role
  email: string
  language: string
  password: string
}

const empty: FormValues = { username: '', displayName: '', role: 'child', email: '', language: 'sv', password: '' }

/** Adds a Member (`/members/new`) or edits, sets the password of and removes one (`/members/:id`). */
export function MemberFormPage() {
  const { id } = useParams()
  const { t } = useTranslation()
  const { api, member: me, reloadMember } = useAuth()
  const navigate = useNavigate()
  const [original, setOriginal] = useState<Member>()
  const [values, setValues] = useState<FormValues>(empty)
  const [error, setError] = useState<unknown>()
  const [busy, setBusy] = useState(false)
  const [newPassword, setNewPassword] = useState('')
  const [passwordError, setPasswordError] = useState<unknown>()
  const [passwordSaved, setPasswordSaved] = useState(false)
  const { ref: removeDialogRef, show: showRemoveDialog, hide: hideRemoveDialog } = useBootstrapModal()

  useEffect(() => {
    if (!id) return
    api<Member>(`/api/members/${id}`).then((member) => {
      setOriginal(member)
      setValues({
        username: member.username,
        displayName: member.displayName,
        role: member.role,
        email: member.email ?? '',
        language: member.preferredLanguage,
        password: '',
      })
    }, setError)
  }, [id, api])

  const typedUsername = values.username.trim().toLowerCase()
  const availability = useUsernameAvailability(original && typedUsername === original.username ? '' : values.username)

  if (id && !original) {
    return error === undefined ? <Loading /> : <div className="alert alert-danger">{describeError(t, error).form}</div>
  }

  const isNew = !original
  const promoting = original?.role === 'child' && values.role === 'parent'
  const needsPassword = isNew || promoting
  const set = (field: keyof FormValues) => (event: { target: { value: string } }) =>
    setValues((current) => ({ ...current, [field]: event.target.value }))

  const body = () => ({
    username: values.username,
    displayName: values.displayName || null,
    role: values.role,
    email: values.role === 'parent' ? values.email : null,
    language: values.language,
    password: needsPassword ? values.password : null,
  })

  const afterChange = async () => {
    // Editing yourself can change your name, language or role.
    if (original?.id === me?.memberId) await reloadMember()
    navigate('/members')
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(undefined)
    try {
      await api(isNew ? '/api/members' : `/api/members/${original.id}`, { method: isNew ? 'POST' : 'PUT', body: body() })
      await afterChange()
    } catch (e) {
      setError(e)
      setBusy(false)
    }
  }

  const setPassword = async (event: FormEvent) => {
    event.preventDefault()
    setPasswordError(undefined)
    setPasswordSaved(false)
    try {
      await api(`/api/members/${original!.id}/password`, { method: 'PUT', body: { password: newPassword } })
      setNewPassword('')
      setPasswordSaved(true)
    } catch (e) {
      setPasswordError(e)
    }
  }

  const remove = async () => {
    hideRemoveDialog()
    setError(undefined)
    try {
      await api(`/api/members/${original!.id}`, { method: 'DELETE' })
      navigate('/members')
    } catch (e) {
      setError(e)
    }
  }

  const minimum = minimumPasswordLength[values.role]
  const errors = describeError(t, error, { min: minimum })
  const passwordErrors = describeError(t, passwordError, { min: minimumPasswordLength[original?.role ?? 'child'] })
  const usernameError =
    errors.field('username') ??
    (availability === 'taken' ? t('errors.username.taken') : availability === 'invalid' ? t('errors.username.invalid') : undefined)

  return (
    <div className="row justify-content-center">
      <div className="col-md-9 col-lg-7">
        <p className="mb-2">
          <Link to="/members">← {t('members.back')}</Link>
        </p>
        <h2 className="h3 mb-3">{isNew ? t('members.addTitle') : t('members.editTitle', { name: original.displayName })}</h2>
        {errors.form && (
          <div className="alert alert-danger" role="alert">
            {errors.form}
          </div>
        )}
        <form onSubmit={submit} noValidate>
          <SelectField label={t('fields.role')} value={values.role} onChange={set('role')} error={errors.field('role')}>
            <option value="child">{t('roles.child')}</option>
            <option value="parent">{t('roles.parent')}</option>
          </SelectField>
          <TextField
            label={t('fields.username')}
            autoCapitalize="none"
            spellCheck={false}
            value={values.username}
            onChange={set('username')}
            error={usernameError}
            hint={t('fields.usernameHint')}
            required
          />
          <TextField label={t('fields.displayName')} value={values.displayName} onChange={set('displayName')} error={errors.field('displayName')} hint={t('fields.displayNameHint')} />
          {values.role === 'parent' && (
            <TextField label={t('fields.email')} type="email" value={values.email} onChange={set('email')} error={errors.field('email')} hint={t('fields.emailHint')} required />
          )}
          <SelectField label={t('language.label')} value={values.language} onChange={set('language')} error={errors.field('language')}>
            {supportedLanguages.map((lng) => (
              <option key={lng} value={lng}>
                {t(`language.${lng}`)}
              </option>
            ))}
          </SelectField>
          {needsPassword && (
            <TextField
              label={promoting ? t('members.newParentPassword') : t('fields.password')}
              type="password"
              autoComplete="new-password"
              value={values.password}
              onChange={set('password')}
              error={errors.field('password')}
              hint={t('fields.passwordHint', { min: minimum })}
              required
            />
          )}
          <button type="submit" className="btn btn-primary" disabled={busy}>
            {isNew ? t('members.add') : t('common.save')}
          </button>
        </form>

        {!isNew && (
          <>
            <hr className="my-4" />
            <form onSubmit={setPassword} noValidate>
              <h3 className="h5">{t('members.setPasswordTitle')}</h3>
              {passwordSaved && (
                <div className="alert alert-success py-2" role="status">
                  {t('members.passwordSaved')}
                </div>
              )}
              <TextField
                label={t('members.newPassword')}
                type="password"
                autoComplete="new-password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                error={passwordErrors.field('password') ?? passwordErrors.form}
                hint={t('members.setPasswordHint', { min: minimumPasswordLength[original.role] })}
              />
              <button type="submit" className="btn btn-outline-primary" disabled={!newPassword}>
                {t('members.setPassword')}
              </button>
            </form>

            <hr className="my-4" />
            <button type="button" className="btn btn-outline-danger" onClick={showRemoveDialog}>
              {t('members.remove')}
            </button>
            <ConfirmDialog
              ref={removeDialogRef}
              title={t('members.removeTitle', { name: original.displayName })}
              body={t('members.removeBody')}
              confirmLabel={t('members.remove')}
              onConfirm={() => void remove()}
            />
          </>
        )}
      </div>
    </div>
  )
}
