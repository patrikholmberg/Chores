import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useAuth } from '../auth/AuthContext'
import { describeError } from '../forms/errors'
import { Loading } from '../shell/Loading'
import { MultiplierBadge } from './ChoresPage'
import type { ChoreSummary } from './chores'

/** Archived Chores, each of which a Parent can put back in the library. */
export function ArchivedChoresPage() {
  const { t } = useTranslation()
  const { api } = useAuth()
  const [chores, setChores] = useState<ChoreSummary[]>()
  const [error, setError] = useState<unknown>()
  const [restored, setRestored] = useState<string>()

  useEffect(() => {
    api<ChoreSummary[]>('/api/chores/archived').then(setChores, setError)
  }, [api])

  const restore = async (chore: ChoreSummary) => {
    setError(undefined)
    setRestored(undefined)
    try {
      await api(`/api/chores/${chore.id}/restore`, { method: 'POST' })
      setChores((current) => current?.filter((c) => c.id !== chore.id))
      setRestored(chore.title)
    } catch (e) {
      setError(e)
    }
  }

  return (
    <>
      <p className="mb-2">
        <Link to="/chores">← {t('chores.back')}</Link>
      </p>
      <h2 className="h3 mb-3">{t('chores.archivedTitle')}</h2>
      {restored && (
        <div className="alert alert-success py-2" role="status">
          {t('chores.restored', { title: restored })}
        </div>
      )}
      {error !== undefined && (
        <div className="alert alert-danger" role="alert">
          {describeError(t, error).form}
        </div>
      )}
      {!chores ? (
        error === undefined && <Loading />
      ) : chores.length === 0 ? (
        <p className="text-body-secondary">{t('chores.archivedEmpty')}</p>
      ) : (
        <ul className="list-group">
          {chores.map((chore) => (
            <li key={chore.id} className="list-group-item d-flex justify-content-between align-items-center gap-2">
              <Link to={`/chores/${chore.id}`} className="text-reset">
                <span className="fw-semibold">{chore.title}</span>
              </Link>
              <span className="d-flex align-items-center gap-2">
                <MultiplierBadge value={chore.pointMultiplier} />
                <button
                  type="button"
                  className="btn btn-sm btn-outline-primary"
                  onClick={() => void restore(chore)}
                  aria-label={`${t('chores.restore')} ${chore.title}`}
                >
                  {t('chores.restore')}
                </button>
              </span>
            </li>
          ))}
        </ul>
      )}
    </>
  )
}
