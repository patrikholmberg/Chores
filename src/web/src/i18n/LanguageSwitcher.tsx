import { useTranslation } from 'react-i18next'
import { supportedLanguages } from '.'

export function LanguageSwitcher() {
  const { t, i18n } = useTranslation()

  return (
    <label className="language-switcher">
      {t('language.label')}{' '}
      <select
        value={i18n.resolvedLanguage}
        onChange={(event) => void i18n.changeLanguage(event.target.value)}
      >
        {supportedLanguages.map((lng) => (
          <option key={lng} value={lng}>
            {t(`language.${lng}`)}
          </option>
        ))}
      </select>
    </label>
  )
}
