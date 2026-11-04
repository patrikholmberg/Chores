import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { supportedLanguages } from '.'
import { useAuth } from '../auth/AuthContext'

export function LanguageSwitcher() {
  const { t, i18n } = useTranslation()
  const { status, api, reloadMember } = useAuth()
  const id = useId()

  const change = async (language: string) => {
    await i18n.changeLanguage(language)
    if (status === 'signedIn') {
      // Saved on the Member, so every device they sign in on follows.
      await api('/api/me/language', { method: 'PUT', body: { language } })
      await reloadMember()
    }
  }

  return (
    <div className="d-flex align-items-center gap-2">
      <label htmlFor={id} className="form-label mb-0 small text-body-secondary">
        {t('language.label')}
      </label>
      <select
        id={id}
        className="form-select form-select-sm w-auto"
        value={i18n.resolvedLanguage}
        onChange={(event) => void change(event.target.value).catch(() => undefined)}
      >
        {supportedLanguages.map((lng) => (
          <option key={lng} value={lng}>
            {t(`language.${lng}`)}
          </option>
        ))}
      </select>
    </div>
  )
}
