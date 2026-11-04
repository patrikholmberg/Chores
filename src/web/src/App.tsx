import { useTranslation } from 'react-i18next'
import { LanguageSwitcher } from './i18n/LanguageSwitcher'
import { OfflineBanner } from './shell/OfflineBanner'

export default function App() {
  const { t } = useTranslation()

  return (
    <main className="shell">
      <OfflineBanner />
      <header className="shell-header">
        <h1>{t('app.title')}</h1>
        <LanguageSwitcher />
      </header>
    </main>
  )
}
