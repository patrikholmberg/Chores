import { useTranslation } from 'react-i18next'
import { useAuth } from '../auth/AuthContext'

/** Placeholder until the Chore Library and points screens exist. */
export function HomePage() {
  const { t } = useTranslation()
  const { member } = useAuth()

  return (
    <div className="py-4">
      <h2 className="h3">{t('home.greeting', { name: member?.displayName ?? '' })}</h2>
      {member && <p className="text-body-secondary">{t('home.family', { family: member.familyName })}</p>}
    </div>
  )
}
