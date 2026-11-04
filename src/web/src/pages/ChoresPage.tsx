import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useAuth } from '../auth/AuthContext'
import { describeError } from '../forms/errors'
import { Loading } from '../shell/Loading'
import type { ChoreSummary } from './chores'

/** The Chore Library for every Member; Parents also get to add Chores and open the archive. */
export function ChoresPage() {
  const { t } = useTranslation()
  const { api, member } = useAuth()
  const [chores, setChores] = useState<ChoreSummary[]>()
  const [error, setError] = useState<unknown>()
  const isParent = member?.role === 'parent'

  useEffect(() => {
    api<ChoreSummary[]>('/api/chores').then(setChores, setError)
  }, [api])

  return (
    <>
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
        <h2 className="h3 mb-0">{t('chores.title')}</h2>
        {isParent && (
          <div className="d-flex gap-2">
            <Link to="/chores/archived" className="btn btn-outline-secondary">
              {t('chores.archived')}
            </Link>
            <Link to="/chores/new" className="btn btn-primary">
              {t('chores.new')}
            </Link>
          </div>
        )}
      </div>
      {error !== undefined ? (
        <div className="alert alert-danger" role="alert">
          {describeError(t, error).form}
        </div>
      ) : !chores ? (
        <Loading />
      ) : chores.length === 0 ? (
        <p className="text-body-secondary">{isParent ? t('chores.emptyParent') : t('chores.empty')}</p>
      ) : (
        <ChoreList chores={chores} />
      )}
    </>
  )
}

function ChoreList({ chores }: { chores: ChoreSummary[] }) {
  const { t } = useTranslation()
  return (
    <div className="list-group">
      {chores.map((chore) => (
        <Link
          key={chore.id}
          to={`/chores/${chore.id}`}
          className="list-group-item list-group-item-action d-flex justify-content-between align-items-center gap-2"
        >
          <span>
            <span className="fw-semibold">{chore.title}</span>{' '}
            <span className="text-body-secondary small">{t('chores.steps', { count: chore.stepCount })}</span>
          </span>
          <MultiplierBadge value={chore.pointMultiplier} />
        </Link>
      ))}
    </div>
  )
}

/** The Point Multiplier as a badge, e.g. "×2", with a spelled-out label for screen readers. */
export function MultiplierBadge({ value }: { value: number }) {
  const { t } = useTranslation()
  return (
    <span className="badge text-bg-primary" title={t('chores.multiplierLabel', { value })} aria-label={t('chores.multiplierLabel', { value })}>
      {t('chores.multiplier', { value })}
    </span>
  )
}
