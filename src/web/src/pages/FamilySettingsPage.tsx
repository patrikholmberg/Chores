import { useEffect, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../auth/AuthContext'
import { describeError } from '../forms/errors'
import { TextField } from '../forms/TextField'
import { Loading } from '../shell/Loading'
import type { FamilySettings } from './attempts'

/** Parents set the Family's Base Points and how many days a started chore may take. */
export function FamilySettingsPage() {
  const { t } = useTranslation()
  const { api } = useAuth()
  const [basePoints, setBasePoints] = useState('')
  const [attemptWindowDays, setAttemptWindowDays] = useState('')
  const [loaded, setLoaded] = useState(false)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<unknown>()

  useEffect(() => {
    api<FamilySettings>('/api/family/settings').then((settings) => {
      setBasePoints(String(settings.basePoints))
      setAttemptWindowDays(String(settings.attemptWindowDays))
      setLoaded(true)
    }, setError)
  }, [api])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setSaving(true)
    setSaved(false)
    setError(undefined)
    try {
      const settings = await api<FamilySettings>('/api/family/settings', {
        method: 'PUT',
        body: { basePoints: Number(basePoints), attemptWindowDays: Number(attemptWindowDays) },
      })
      setBasePoints(String(settings.basePoints))
      setAttemptWindowDays(String(settings.attemptWindowDays))
      setSaved(true)
    } catch (e) {
      setError(e)
    } finally {
      setSaving(false)
    }
  }

  const errors = describeError(t, error)

  return (
    <div className="row justify-content-center">
      <div className="col-md-8 col-lg-6">
        <h2 className="h3 mb-3">{t('settings.title')}</h2>
        {errors.form && (
          <div className="alert alert-danger" role="alert">
            {errors.form}
          </div>
        )}
        {saved && (
          <div className="alert alert-success" role="status">
            {t('settings.saved')}
          </div>
        )}
        {!loaded ? (
          !errors.form && <Loading />
        ) : (
          <form onSubmit={(e) => void submit(e)} noValidate>
            <TextField
              label={t('settings.basePoints')}
              type="number"
              inputMode="numeric"
              min={1}
              max={1000}
              step={1}
              required
              value={basePoints}
              onChange={(e) => setBasePoints(e.target.value)}
              error={errors.field('basePoints')}
              hint={t('settings.basePointsHint')}
            />
            <TextField
              label={t('settings.attemptWindow')}
              type="number"
              inputMode="numeric"
              min={1}
              max={30}
              step={1}
              required
              value={attemptWindowDays}
              onChange={(e) => setAttemptWindowDays(e.target.value)}
              error={errors.field('attemptWindowDays')}
              hint={t('settings.attemptWindowHint')}
            />
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {t('common.save')}
            </button>
          </form>
        )}
      </div>
    </div>
  )
}
