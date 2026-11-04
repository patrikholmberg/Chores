import { useTranslation } from 'react-i18next'
import { useOnlineStatus } from './useOnlineStatus'

export function OfflineBanner() {
  const { t } = useTranslation()
  const online = useOnlineStatus()

  if (online) {
    return null
  }

  return (
    <p role="status" className="offline-banner">
      {t('offline.message')}
    </p>
  )
}
