import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router'
import { useAuth } from '../auth/AuthContext'
import { describeError } from '../forms/errors'
import { ConfirmDialog } from '../shell/ConfirmDialog'
import { Loading } from '../shell/Loading'
import { useBootstrapModal } from '../shell/useBootstrapModal'
import { MultiplierBadge } from './ChoresPage'
import { formatDuration, type Chore, type Step } from './chores'

/** One Chore with its Steps. Parents can edit, archive or restore it from here. */
export function ChorePage() {
  const { id } = useParams()
  const { t } = useTranslation()
  const { api, member } = useAuth()
  const navigate = useNavigate()
  const [chore, setChore] = useState<Chore>()
  const [error, setError] = useState<unknown>()
  const { ref: archiveDialogRef, show: showArchiveDialog, hide: hideArchiveDialog } = useBootstrapModal()
  const isParent = member?.role === 'parent'

  useEffect(() => {
    api<Chore>(`/api/chores/${id}`).then(setChore, setError)
  }, [id, api])

  const archive = async () => {
    hideArchiveDialog()
    setError(undefined)
    try {
      await api(`/api/chores/${id}`, { method: 'DELETE' })
      navigate('/chores')
    } catch (e) {
      setError(e)
    }
  }

  const restore = async () => {
    setError(undefined)
    try {
      await api(`/api/chores/${id}/restore`, { method: 'POST' })
      setChore(await api<Chore>(`/api/chores/${id}`))
    } catch (e) {
      setError(e)
    }
  }

  const errorAlert = error !== undefined && (
    <div className="alert alert-danger" role="alert">
      {describeError(t, error).form}
    </div>
  )

  if (!chore) {
    return errorAlert || <Loading />
  }

  return (
    <div className="row justify-content-center">
      <div className="col-md-9 col-lg-7">
        <p className="mb-2">
          <Link to="/chores">← {t('chores.back')}</Link>
        </p>
        <div className="d-flex align-items-center gap-2 mb-2">
          <h2 className="h3 mb-0">{chore.title}</h2>
          <MultiplierBadge value={chore.pointMultiplier} />
        </div>
        {errorAlert}
        {chore.archived && (
          <div className="alert alert-secondary d-flex flex-wrap align-items-center justify-content-between gap-2" role="status">
            {t('chores.archivedNotice')}
            {isParent && (
              <button type="button" className="btn btn-sm btn-primary" onClick={() => void restore()}>
                {t('chores.restore')}
              </button>
            )}
          </div>
        )}
        {chore.description && <p className="text-body-secondary" style={{ whiteSpace: 'pre-line' }}>{chore.description}</p>}

        <ol className="list-group list-group-numbered mb-3" aria-label={t('chores.stepsTitle')}>
          {chore.steps.map((step, index) => (
            <StepItem key={index} step={step} />
          ))}
        </ol>

        {isParent && !chore.archived && (
          <div className="d-flex gap-2">
            <Link to={`/chores/${chore.id}/edit`} className="btn btn-primary">
              {t('chores.edit')}
            </Link>
            <button type="button" className="btn btn-outline-danger" onClick={showArchiveDialog}>
              {t('chores.archive')}
            </button>
            <ConfirmDialog
              ref={archiveDialogRef}
              title={t('chores.archiveTitle', { title: chore.title })}
              body={t('chores.archiveBody')}
              confirmLabel={t('chores.archive')}
              onConfirm={() => void archive()}
            />
          </div>
        )}
      </div>
    </div>
  )
}

function StepItem({ step }: { step: Step }) {
  const { t } = useTranslation()
  const reminder =
    step.reminderDelayMinutes !== null &&
    [
      t('chores.reminder', { delay: formatDuration(t, step.reminderDelayMinutes) }),
      step.repeatIntervalMinutes !== null && t('chores.repeats', { interval: formatDuration(t, step.repeatIntervalMinutes) }),
    ]
      .filter(Boolean)
      .join(', ')

  return (
    <li className="list-group-item d-flex">
      <div className="ms-2">
        <div className="fw-semibold">{step.title}</div>
        {step.description && <div style={{ whiteSpace: 'pre-line' }}>{step.description}</div>}
        {reminder && <div className="small text-body-secondary">{reminder}</div>}
      </div>
    </li>
  )
}
