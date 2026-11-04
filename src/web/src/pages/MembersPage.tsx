import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useAuth } from '../auth/AuthContext'
import { describeError } from '../forms/errors'
import { Loading } from '../shell/Loading'
import type { Member } from './members'

export function MembersPage() {
  const { t } = useTranslation()
  const { api } = useAuth()
  const [members, setMembers] = useState<Member[]>()
  const [error, setError] = useState<unknown>()

  useEffect(() => {
    api<Member[]>('/api/members').then(setMembers, setError)
  }, [api])

  return (
    <>
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
        <h2 className="h3 mb-0">{t('members.title')}</h2>
        <Link to="/members/new" className="btn btn-primary">
          {t('members.add')}
        </Link>
      </div>
      {error !== undefined ? (
        <div className="alert alert-danger" role="alert">
          {describeError(t, error).form}
        </div>
      ) : !members ? (
        <Loading />
      ) : (
        <div className="list-group">
          {members.map((member) => (
            <Link
              key={member.id}
              to={`/members/${member.id}`}
              className="list-group-item list-group-item-action d-flex justify-content-between align-items-center gap-2"
            >
              <span>
                <span className="fw-semibold">{member.displayName}</span>{' '}
                <span className="text-body-secondary small">@{member.username}</span>
              </span>
              <span className={`badge ${member.role === 'parent' ? 'text-bg-primary' : 'text-bg-secondary'}`}>
                {t(`roles.${member.role}`)}
              </span>
            </Link>
          ))}
        </div>
      )}
    </>
  )
}
