import { useTranslation } from 'react-i18next'
import { useOnlineStatus } from './useOnlineStatus'

export function OfflineBanner() {
  const { t } = useTranslation()
  const online = useOnlineStatus()

  if (online) {
    return null
  }

  return (
    <p role="status" className="alert alert-warning py-2 mb-3">
      {t('offline.message')}
    </p>
  )
}
