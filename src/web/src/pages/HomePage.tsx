import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useAuth } from '../auth/AuthContext'
import { describeError } from '../forms/errors'
import { canReject, formatDay, stateBadge, type AttemptSummary, type TodayPoints } from './attempts'

/** Today's points and the Chore Attempts that matter now: a Child's own, or every Child's for a Parent. */
export function HomePage() {
  const { t } = useTranslation()
  const { api, member } = useAuth()
  const [today, setToday] = useState<TodayPoints>()
  const [attempts, setAttempts] = useState<AttemptSummary[]>()
  const [error, setError] = useState<unknown>()

  useEffect(() => {
    if (!member) {
      return
    }
    // Reloaded on every visit, so the points are current after any action elsewhere.
    Promise.all([api<TodayPoints>('/api/points/today'), api<AttemptSummary[]>('/api/attempts')]).then(([points, list]) => {
      setToday(points)
      setAttempts(list)
    }, setError)
  }, [api, member])

  return (
    <div className="py-4">
      <h2 className="h3">{t('home.greeting', { name: member?.displayName ?? '' })}</h2>
      {member && <p className="text-body-secondary">{t('home.family', { family: member.familyName })}</p>}
      {error !== undefined && (
        <div className="alert alert-danger" role="alert">
          {describeError(t, error).form}
        </div>
      )}
      {member?.role === 'child' ? (
        <ChildHome today={today} attempts={attempts} />
      ) : (
        <>
          <ParentHome today={today} attempts={attempts} />
          <Link to="/chores" className="btn btn-primary">
            {t('home.library')}
          </Link>
        </>
      )}
    </div>
  )
}

interface HomeProps {
  today: TodayPoints | undefined
  attempts: AttemptSummary[] | undefined
}

function ChildHome({ today, attempts }: HomeProps) {
  const { t, i18n } = useTranslation()
  const points = today?.children[0]?.points
  const inProgress = attempts?.filter((a) => a.state === 'open') ?? []
  const doneToday = attempts?.filter((a) => a.startDay === today?.day && (a.state === 'complete' || a.state === 'rejected')) ?? []

  return (
    <>
      <section className="card mb-3" aria-label={t('home.today')}>
        <div className="card-body d-flex flex-wrap align-items-center justify-content-between gap-2">
          <div>
            <div className="text-body-secondary small">{t('home.today')}</div>
            {points !== undefined && <div className="fs-2 fw-bold">{t('attempts.points', { count: points })}</div>}
          </div>
          <Link to="/chores" className="btn btn-primary btn-lg">
            {t('home.doChore')}
          </Link>
        </div>
      </section>

      {attempts && (
        <>
          <h3 className="h5">{t('home.inProgress')}</h3>
          {inProgress.length === 0 ? (
            <p className="text-body-secondary">{t('home.nothingInProgress')}</p>
          ) : (
            <div className="list-group mb-3" aria-label={t('home.inProgress')}>
              {inProgress.map((attempt) => (
                <Link key={attempt.id} to={`/attempts/${attempt.id}`} className="list-group-item list-group-item-action">
                  <div className="fw-semibold">{attempt.title}</div>
                  <div className="small text-body-secondary">
                    {attempt.currentStepTitle && t('attempts.current', { title: attempt.currentStepTitle })}
                    {attempt.startDay !== today?.day && <> · {t('attempts.startedOn', { date: formatDay(attempt.startDay, i18n.language) })}</>}
                  </div>
                </Link>
              ))}
            </div>
          )}

          {doneToday.length > 0 && (
            <>
              <h3 className="h5">{t('home.doneToday')}</h3>
              <div className="list-group mb-3" aria-label={t('home.doneToday')}>
                {doneToday.map((attempt) => (
                  <Link
                    key={attempt.id}
                    to={`/attempts/${attempt.id}`}
                    className="list-group-item list-group-item-action d-flex justify-content-between align-items-center gap-2"
                  >
                    {attempt.title}
                    <span className={`badge ${stateBadge[attempt.state]}`}>{t(`attempts.state.${attempt.state}`)}</span>
                  </Link>
                ))}
              </div>
            </>
          )}
        </>
      )}
    </>
  )
}

function ParentHome({ today, attempts }: HomeProps) {
  const { t, i18n } = useTranslation()

  return (
    <>
      {today && today.children.length > 0 && (
        <section className="card mb-3" aria-label={t('home.today')}>
          <div className="card-body">
            <div className="text-body-secondary small mb-1">{t('home.today')}</div>
            <ul className="list-unstyled mb-0">
              {today.children.map((c) => (
                <li key={c.childId} className="d-flex justify-content-between gap-2">
                  <span>{c.childName}</span>
                  <span className="fw-bold">{t('attempts.points', { count: c.points })}</span>
                </li>
              ))}
            </ul>
          </div>
        </section>
      )}

      {attempts && (
        <>
          <h3 className="h5">{t('home.recent')}</h3>
          {attempts.length === 0 ? (
            <p className="text-body-secondary">{t('home.noRecent')}</p>
          ) : (
            <ul className="list-group mb-3" aria-label={t('home.recent')}>
              {attempts.map((attempt) => (
                <li key={attempt.id} className="list-group-item d-flex flex-wrap align-items-center gap-2">
                  <div className="me-auto">
                    <Link to={`/attempts/${attempt.id}`} className="fw-semibold">
                      {attempt.title}
                    </Link>
                    <div className="small text-body-secondary">
                      {attempt.childName} · {formatDay(attempt.startDay, i18n.language)}
                    </div>
                  </div>
                  <span className={`badge ${stateBadge[attempt.state]}`}>{t(`attempts.state.${attempt.state}`)}</span>
                  {canReject(attempt) && (
                    <Link to={`/attempts/${attempt.id}`} className="btn btn-sm btn-outline-danger">
                      {t('attempts.reject')}
                    </Link>
                  )}
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </>
  )
}
