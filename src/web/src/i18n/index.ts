import i18n from 'i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import { initReactI18next } from 'react-i18next'
import en from '../locales/en/common.json'
import sv from '../locales/sv/common.json'

export const supportedLanguages = ['sv', 'en'] as const
export type SupportedLanguage = (typeof supportedLanguages)[number]

export const languageStorageKey = 'chores.language'

export const resources = {
  en: { common: en },
  sv: { common: sv },
} as const

export function createI18n() {
  const instance = i18n.createInstance()

  void instance
    .use(LanguageDetector)
    .use(initReactI18next)
    .init({
      resources,
      defaultNS: 'common',
      supportedLngs: supportedLanguages,
      nonExplicitSupportedLngs: true,
      load: 'languageOnly',
      fallbackLng: 'en',
      interpolation: { escapeValue: false },
      detection: {
        order: ['localStorage', 'navigator'],
        lookupLocalStorage: languageStorageKey,
        caches: ['localStorage'],
      },
    })

  instance.on('languageChanged', (lng) => {
    document.documentElement.lang = lng
  })
  document.documentElement.lang = instance.resolvedLanguage ?? 'en'

  return instance
}
