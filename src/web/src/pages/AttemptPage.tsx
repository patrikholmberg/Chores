import { useCallback, useEffect, useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/ApiError'
import { useAuth } from '../auth/AuthContext'
import { describeError } from '../forms/errors'
import { ConfirmDialog } from '../shell/ConfirmDialog'
import { Loading } from '../shell/Loading'
import { useBootstrapModal } from '../shell/useBootstrapModal'
import { canReject, formatDay, formatInstant, stateBadge, type Attempt, type AttemptStep } from './attempts'
import { MultiplierBadge } from './ChoresPage'

/**
 * One Chore Attempt. The Child who owns it ticks off its Steps in order, starts it over or cancels it; a Parent can reject
 * it once complete, until its attempt window has passed.
 */
export function AttemptPage() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const { api, member } = useAuth()
  const navigate = useNavigate()
  const [attempt, setAttempt] = useState<Attempt>()
  const [error, setError] = useState<unknown>()
  const [busy, setBusy] = useState(false)
  const [reason, setReason] = useState('')
  const { ref: restartDialogRef, show: showRestartDialog, hide: hideRestartDialog } = useBootstrapModal()
  const { ref: cancelDialogRef, show: showCancelDialog, hide: hideCancelDialog } = useBootstrapModal()
  const { ref: rejectDialogRef, show: showRejectDialog, hide: hideRejectDialog } = useBootstrapModal()
  const reasonId = useId()
  const rejectTitleId = useId()

  const load = useCallback(() => api<Attempt>(`/api/attempts/${id}`).then(setAttempt, setError), [api, id])

  useEffect(() => {
    void load()
  }, [load])

  /** Runs an action; on a conflict the attempt changed elsewhere, so it is reloaded and the reason shown. */
  const act = async (action: () => Promise<void>) => {
    setBusy(true)
    setError(undefined)
    try {
      await action()
    } catch (e) {
      setError(e)
      if (e instanceof ApiError && e.status === 409) {
        await load()
      }
    } finally {
      setBusy(false)
    }
  }

  const completeStep = (position: number) =>
    act(async () => setAttempt(await api<Attempt>(`/api/attempts/${id}/steps/${position}/complete`, { method: 'POST' })))

  const restart = () => {
    hideRestartDialog()
    return act(async () => setAttempt(await api<Attempt>(`/api/attempts/${id}/restart`, { method: 'POST' })))
  }

  const cancel = () => {
    hideCancelDialog()
    return act(async () => {
      await api(`/api/attempts/${id}`, { method: 'DELETE' })
      navigate('/')
    })
  }

  const reject = () => {
    hideRejectDialog()
    return act(async () => {
      setAttempt(await api<Attempt>(`/api/attempts/${id}/reject`, { method: 'POST', body: { reason: reason.trim() || null } }))
      setReason('')
    })
  }

  const errorAlert = error !== undefined && (
    <div className="alert alert-danger" role="alert">
      {describeError(t, error).form}
    </div>
  )

  if (!attempt) {
    return errorAlert || <Loading />
  }

  const timeZone = member?.timeZone ?? 'UTC'
  const isOwner = member?.memberId === attempt.childId
  const isOpen = attempt.state === 'open'
  const rejectable = member?.role === 'parent' && canReject(attempt)

  return (
    <div className="row justify-content-center">
      <div className="col-md-9 col-lg-7">
        <p className="mb-2">
          <Link to="/">← {t('attempts.back')}</Link>
        </p>
        <div className="d-flex flex-wrap align-items-center gap-2 mb-1">
          <h2 className="h3 mb-0">{attempt.title}</h2>
          <MultiplierBadge value={attempt.pointMultiplier} />
          <span className={`badge ${stateBadge[attempt.state]}`}>{t(`attempts.state.${attempt.state}`)}</span>
        </div>
        <p className="text-body-secondary small mb-3">
          {!isOwner && <>{attempt.childName} · </>}
          {t('attempts.startedOn', { date: formatDay(attempt.startDay, i18n.language) })}
          {isOpen && <> · {t('attempts.expires', { date: formatInstant(attempt.expiresAt, i18n.language, timeZone) })}</>}
        </p>
        {errorAlert}
        {attempt.state === 'abandoned' && (
          <div className="alert alert-secondary" role="status">
            {t('attempts.abandonedNotice')}
          </div>
        )}
        {attempt.rejection && (
          <div className="alert alert-danger" role="status">
            {t('attempts.rejectedBy', { name: attempt.rejection.byName })}
            {attempt.rejection.reason && (
              <div style={{ whiteSpace: 'pre-line' }}>{t('attempts.rejectedReason', { reason: attempt.rejection.reason })}</div>
            )}
          </div>
        )}
        {attempt.description && <p style={{ whiteSpace: 'pre-line' }}>{attempt.description}</p>}

        <ol className="list-group list-group-numbered mb-3" aria-label={t('attempts.stepsTitle')}>
          {attempt.steps.map((step) => (
            <StepItem
              key={step.position}
              step={step}
              current={isOpen && step.position === attempt.currentStepPosition}
              canComplete={isOwner && isOpen && step.position === attempt.currentStepPosition}
              busy={busy}
              onComplete={() => void completeStep(step.position)}
              timeZone={timeZone}
            />
          ))}
        </ol>

        {(isOwner || member?.role === 'parent') && isOpen && (
          <div className="d-flex flex-wrap gap-2">
            {isOwner && (
              <>
                <button type="button" className="btn btn-outline-secondary" disabled={busy} onClick={showRestartDialog}>
                  {t('attempts.restart')}
                </button>
                <ConfirmDialog
                  ref={restartDialogRef}
                  title={t('attempts.restartTitle', { title: attempt.title })}
                  body={t('attempts.restartBody')}
                  confirmLabel={t('attempts.restart')}
                  onConfirm={() => void restart()}
                />
              </>
            )}
            <button type="button" className="btn btn-outline-danger" disabled={busy} onClick={showCancelDialog}>
              {t('attempts.cancel')}
            </button>
            <ConfirmDialog
              ref={cancelDialogRef}
              title={t('attempts.cancelTitle', { title: attempt.title })}
              body={t('attempts.cancelBody')}
              confirmLabel={t('attempts.cancel')}
              onConfirm={() => void cancel()}
            />
          </div>
        )}

        {rejectable && (
          <div className="d-flex flex-wrap align-items-center gap-2">
            <button type="button" className="btn btn-outline-danger" disabled={busy} onClick={showRejectDialog}>
              {t('attempts.reject')}
            </button>
            <span className="small text-body-secondary">
              {t('attempts.rejectUntil', { date: formatInstant(attempt.expiresAt, i18n.language, timeZone) })}
            </span>
            <div className="modal fade" tabIndex={-1} ref={rejectDialogRef} aria-labelledby={rejectTitleId} aria-hidden="true">
              <div className="modal-dialog modal-dialog-centered">
                <div className="modal-content">
                  <div className="modal-header">
                    <h2 className="modal-title fs-5" id={rejectTitleId}>
                      {t('attempts.rejectTitle', { title: attempt.title })}
                    </h2>
                    <button type="button" className="btn-close" data-bs-dismiss="modal" aria-label={t('common.close')} />
                  </div>
                  <div className="modal-body">
                    <label htmlFor={reasonId} className="form-label">
                      {t('attempts.rejectReason')}
                    </label>
                    <textarea
                      id={reasonId}
                      className="form-control"
                      rows={3}
                      maxLength={500}
                      value={reason}
                      onChange={(e) => setReason(e.target.value)}
                      aria-describedby={`${reasonId}-hint`}
                    />
                    <div id={`${reasonId}-hint`} className="form-text">
                      {t('attempts.rejectReasonHint', { name: attempt.childName })}
                    </div>
                  </div>
                  <div className="modal-footer">
                    <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">
                      {t('common.cancel')}
                    </button>
                    <button type="button" className="btn btn-danger" onClick={() => void reject()}>
                      {t('attempts.reject')}
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

interface StepItemProps {
  step: AttemptStep
  current: boolean
  canComplete: boolean
  busy: boolean
  onComplete(): void
  timeZone: string
}

function StepItem({ step, current, canComplete, busy, onComplete, timeZone }: StepItemProps) {
  const { t, i18n } = useTranslation()
  return (
    <li
      className={`list-group-item d-flex align-items-start gap-2${current ? ' list-group-item-primary' : ''}`}
      aria-current={current ? 'step' : undefined}
    >
      <div className="ms-2 me-auto">
        <div className={`fw-semibold${step.completedAt ? ' text-decoration-line-through text-body-secondary' : ''}`}>{step.title}</div>
        {step.description && <div style={{ whiteSpace: 'pre-line' }}>{step.description}</div>}
        {step.completedAt && (
          <div className="small text-body-secondary">
            ✓ {t('attempts.doneAt', { time: formatInstant(step.completedAt, i18n.language, timeZone) })}
          </div>
        )}
      </div>
      {canComplete && (
        <button type="button" className="btn btn-success" disabled={busy} onClick={onComplete}>
          {t('attempts.done')}
        </button>
      )}
    </li>
  )
}
