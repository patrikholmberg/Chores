import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { I18nextProvider } from 'react-i18next'
import { createI18n } from '../i18n'

/** Renders with a fresh i18n instance, as a page load would. */
export function renderWithI18n(ui: ReactElement) {
  const i18n = createI18n()
  return { i18n, ...render(<I18nextProvider i18n={i18n}>{ui}</I18nextProvider>) }
}
