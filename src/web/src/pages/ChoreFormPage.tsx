import { useCallback, useEffect, useId, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/ApiError'
import { useAuth } from '../auth/AuthContext'
import { describeError } from '../forms/errors'
import { TextField } from '../forms/TextField'
import { Loading } from '../shell/Loading'
import type { Chore, Step } from './chores'

/** A duration as the Parent types it; both parts empty means "not set". */
interface DurationInput {
  hours: string
  minutes: string
}

interface StepRow {
  key: number
  title: string
  description: string
  delay: DurationInput
  interval: DurationInput
}

interface FormValues {
  title: string
  description: string
  pointMultiplier: string
  steps: StepRow[]
}

const noDuration: DurationInput = { hours: '', minutes: '' }

/** Gives every Step row a stable React key, also across reordering. */
let nextKey = 0

function newRow(step?: Step): StepRow {
  return {
    key: nextKey++,
    title: step?.title ?? '',
    description: step?.description ?? '',
    delay: toDuration(step?.reminderDelayMinutes ?? null),
    interval: toDuration(step?.repeatIntervalMinutes ?? null),
  }
}

function toDuration(minutes: number | null): DurationInput {
  return minutes === null ? noDuration : { hours: String(Math.floor(minutes / 60)), minutes: String(minutes % 60) }
}

function toMinutes({ hours, minutes }: DurationInput): number | null {
  return hours.trim() === '' && minutes.trim() === '' ? null : (Number(hours) || 0) * 60 + (Number(minutes) || 0)
}

/** Creates a Chore (`/chores/new`) or edits one (`/chores/:id/edit`), including adding, removing and reordering Steps. */
export function ChoreFormPage() {
  const { id } = useParams()
  const { t } = useTranslation()
  const { api } = useAuth()
  const navigate = useNavigate()
  const [original, setOriginal] = useState<Chore>()
  const [values, setValues] = useState<FormValues | undefined>(() =>
    id ? undefined : { title: '', description: '', pointMultiplier: '1', steps: [newRow()] },
  )
  const [error, setError] = useState<unknown>()
  const [busy, setBusy] = useState(false)
  const [delayCleared, setDelayCleared] = useState<string>()

  /** Puts the saved Chore in the form, dropping anything typed, e.g. after a conflict. */
  const showChore = useCallback((chore: Chore) => {
    setOriginal(chore)
    setValues({
      title: chore.title,
      description: chore.description ?? '',
      pointMultiplier: String(chore.pointMultiplier),
      steps: chore.steps.map((step) => newRow(step)),
    })
    setError(undefined)
    setDelayCleared(undefined)
  }, [])

  const load = useCallback(() => api<Chore>(`/api/chores/${id}`).then(showChore, setError), [id, api, showChore])

  useEffect(() => {
    if (id) api<Chore>(`/api/chores/${id}`).then(showChore, setError)
  }, [id, api, showChore])

  if (!values) {
    return error === undefined ? <Loading /> : <div className="alert alert-danger">{describeError(t, error).form}</div>
  }

  const setField = (field: 'title' | 'description' | 'pointMultiplier') => (event: { target: { value: string } }) =>
    setValues({ ...values, [field]: event.target.value })

  const updateStep = (index: number, change: Partial<StepRow>) =>
    setValues({ ...values, steps: values.steps.map((row, i) => (i === index ? { ...row, ...change } : row)) })

  /** The first Step can't have a reminder, so one that ends up first loses it, with a note saying so. */
  const setSteps = (steps: StepRow[]) => {
    const [first, ...rest] = steps
    if (first && toMinutes(first.delay) !== null) {
      setDelayCleared(first.title || t('chores.stepNumber', { number: 1 }))
      steps = [{ ...first, delay: noDuration, interval: noDuration }, ...rest]
    }
    setValues({ ...values, steps })
  }

  const move = (index: number, offset: -1 | 1) => {
    const steps = [...values.steps]
    ;[steps[index], steps[index + offset]] = [steps[index + offset], steps[index]]
    setSteps(steps)
  }

  const body = () => {
    const pointMultiplier = values.pointMultiplier.trim() === '' ? null : Number(values.pointMultiplier)
    return {
      ...(original && { revision: original.revision }),
      title: values.title,
      description: values.description || null,
      pointMultiplier,
      steps: values.steps.map((row, index) => {
        const reminderDelayMinutes = index === 0 ? null : toMinutes(row.delay)
        return {
          title: row.title,
          description: row.description || null,
          reminderDelayMinutes,
          repeatIntervalMinutes: reminderDelayMinutes === null ? null : toMinutes(row.interval),
        }
      }),
    }
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(undefined)
    try {
      const saved = await api<Chore>(original ? `/api/chores/${original.id}` : '/api/chores', {
        method: original ? 'PUT' : 'POST',
        body: body(),
      })
      navigate(`/chores/${saved.id}`)
    } catch (e) {
      if (e instanceof ApiError && e.code === 'chore.archived') {
        navigate(`/chores/${original!.id}`)
        return
      }
      setError(e)
      setBusy(false)
    }
  }

  const errors = describeError(t, error)
  const conflict = error instanceof ApiError && error.code === 'concurrency_conflict'

  return (
    <div className="row justify-content-center">
      <div className="col-md-10 col-lg-8">
        <p className="mb-2">
          <Link to={original ? `/chores/${original.id}` : '/chores'}>← {original ? original.title : t('chores.back')}</Link>
        </p>
        <h2 className="h3 mb-3">{original ? t('chores.editTitle', { title: original.title }) : t('chores.newTitle')}</h2>
        {errors.form && (
          <div className="alert alert-danger d-flex flex-wrap align-items-center justify-content-between gap-2" role="alert">
            {errors.form}
            {conflict && (
              <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => void load()}>
                {t('chores.reload')}
              </button>
            )}
          </div>
        )}
        <form onSubmit={submit} noValidate>
          <TextField label={t('chores.fieldTitle')} value={values.title} onChange={setField('title')} error={errors.field('title')} required />
          <TextAreaField
            label={t('chores.fieldDescription')}
            value={values.description}
            onChange={setField('description')}
            error={errors.field('description')}
            hint={t('chores.descriptionHint')}
          />
          <TextField
            label={t('chores.fieldMultiplier')}
            type="number"
            inputMode="numeric"
            min={1}
            max={100}
            step={1}
            value={values.pointMultiplier}
            onChange={setField('pointMultiplier')}
            error={errors.field('pointMultiplier')}
            hint={t('chores.multiplierHint')}
          />

          <h3 className="h5 mt-4">{t('chores.stepsTitle')}</h3>
          <p className="form-text mt-0">{t('chores.stepsHint')}</p>
          {errors.field('steps') && (
            <div className="alert alert-danger py-2" role="alert">
              {errors.field('steps')}
            </div>
          )}
          {delayCleared && (
            <div className="alert alert-info py-2" role="status">
              {t('chores.delayCleared', { title: delayCleared })}
            </div>
          )}
          {values.steps.map((row, index) => {
            const number = index + 1
            const field = (name: string) => errors.field(`steps[${index}].${name}`)
            const hasDelay = toMinutes(row.delay) !== null
            return (
              <fieldset key={row.key} className="card mb-3" aria-label={t('chores.stepNumber', { number })}>
                <div className="card-header d-flex align-items-center justify-content-between gap-2">
                  <span className="h6 mb-0" aria-hidden="true">
                    {t('chores.stepNumber', { number })}
                  </span>
                  <div className="btn-group btn-group-sm">
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      disabled={index === 0}
                      onClick={() => move(index, -1)}
                      aria-label={t('chores.moveUp', { number })}
                      title={t('chores.moveUp', { number })}
                    >
                      ↑
                    </button>
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      disabled={index === values.steps.length - 1}
                      onClick={() => move(index, 1)}
                      aria-label={t('chores.moveDown', { number })}
                      title={t('chores.moveDown', { number })}
                    >
                      ↓
                    </button>
                    <button
                      type="button"
                      className="btn btn-outline-danger"
                      disabled={values.steps.length === 1}
                      onClick={() => setSteps(values.steps.filter((_, i) => i !== index))}
                      aria-label={t('chores.removeStep', { number })}
                      title={t('chores.removeStep', { number })}
                    >
                      ✕
                    </button>
                  </div>
                </div>
                <div className="card-body pb-0">
                  <TextField
                    label={t('chores.stepTitle')}
                    value={row.title}
                    onChange={(e) => updateStep(index, { title: e.target.value })}
                    error={field('title')}
                    required
                  />
                  <TextAreaField
                    label={t('chores.fieldDescription')}
                    value={row.description}
                    onChange={(e) => updateStep(index, { description: e.target.value })}
                    error={field('description')}
                    hint={t('chores.descriptionHint')}
                  />
                  {index > 0 && (
                    <div className="row">
                      <div className="col-sm-6">
                        <DurationField
                          label={t('chores.reminderDelay')}
                          value={row.delay}
                          onChange={(delay) => updateStep(index, { delay })}
                          error={field('reminderDelayMinutes')}
                          hint={t('chores.reminderDelayHint')}
                        />
                      </div>
                      <div className="col-sm-6">
                        <DurationField
                          label={t('chores.repeatInterval')}
                          value={row.interval}
                          onChange={(interval) => updateStep(index, { interval })}
                          error={field('repeatIntervalMinutes')}
                          hint={t('chores.repeatIntervalHint')}
                          disabled={!hasDelay}
                        />
                      </div>
                    </div>
                  )}
                  {index === 0 && field('reminderDelayMinutes') && (
                    <div className="alert alert-danger py-2" role="alert">
                      {field('reminderDelayMinutes')}
                    </div>
                  )}
                </div>
              </fieldset>
            )
          })}
          <div className="d-flex flex-wrap gap-2 mb-4">
            <button
              type="button"
              className="btn btn-outline-primary"
              disabled={values.steps.length >= 20}
              onClick={() => setValues({ ...values, steps: [...values.steps, newRow()] })}
            >
              {t('chores.addStep')}
            </button>
            <button type="submit" className="btn btn-primary ms-auto" disabled={busy}>
              {original ? t('common.save') : t('chores.create')}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

interface TextAreaFieldProps {
  label: string
  value: string
  onChange(event: { target: { value: string } }): void
  error?: string
  hint?: string
}

function TextAreaField({ label, value, onChange, error, hint }: TextAreaFieldProps) {
  const id = useId()
  return (
    <div className="mb-3">
      <label htmlFor={id} className="form-label">
        {label}
      </label>
      <textarea
        id={id}
        rows={2}
        className={`form-control${error ? ' is-invalid' : ''}`}
        value={value}
        onChange={onChange}
        aria-describedby={hint ? `${id}-hint` : undefined}
        aria-invalid={error ? true : undefined}
      />
      {error && <div className="invalid-feedback">{error}</div>}
      {hint && !error && (
        <div id={`${id}-hint`} className="form-text">
          {hint}
        </div>
      )}
    </div>
  )
}

interface DurationFieldProps {
  label: string
  value: DurationInput
  onChange(value: DurationInput): void
  error?: string
  hint?: string
  disabled?: boolean
}

/** Hours and minutes side by side, labelled as one field. */
function DurationField({ label, value, onChange, error, hint, disabled }: DurationFieldProps) {
  const { t } = useTranslation()
  const id = useId()
  const invalid = error ? ' is-invalid' : ''
  return (
    <div className="mb-3" role="group" aria-labelledby={`${id}-label`}>
      <div id={`${id}-label`} className="form-label">
        {label}
      </div>
      <div className={`input-group${invalid ? ' has-validation' : ''}`}>
        <input
          type="number"
          inputMode="numeric"
          min={0}
          max={168}
          className={`form-control${invalid}`}
          aria-label={`${label}: ${t('chores.hours')}`}
          value={value.hours}
          disabled={disabled}
          onChange={(e) => onChange({ ...value, hours: e.target.value })}
        />
        <span className="input-group-text">{t('chores.hours')}</span>
        <input
          type="number"
          inputMode="numeric"
          min={0}
          max={59}
          className={`form-control${invalid}`}
          aria-label={`${label}: ${t('chores.minutes')}`}
          value={value.minutes}
          disabled={disabled}
          onChange={(e) => onChange({ ...value, minutes: e.target.value })}
        />
        <span className="input-group-text">{t('chores.minutes')}</span>
        {error && <div className="invalid-feedback">{error}</div>}
      </div>
      {hint && !error && <div className="form-text">{hint}</div>}
    </div>
  )
}
