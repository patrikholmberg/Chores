import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { languageStorageKey } from './i18n'
import { renderWithI18n } from './test/renderWithI18n'

function setBrowserLanguages(...languages: string[]) {
  vi.spyOn(navigator, 'languages', 'get').mockReturnValue(languages)
  vi.spyOn(navigator, 'language', 'get').mockReturnValue(languages[0])
}

describe('App language', () => {
  beforeEach(() => localStorage.clear())
  afterEach(() => vi.restoreAllMocks())

  it('uses Swedish on first visit when the browser prefers Swedish', () => {
    setBrowserLanguages('sv-SE')
    renderWithI18n(<App />)

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Sysslor')
    expect(document.documentElement.lang).toBe('sv')
  })

  it('falls back to English for an unsupported browser language', () => {
    setBrowserLanguages('de-DE')
    renderWithI18n(<App />)

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Chores')
  })

  it('switches immediately and remembers the choice after a reload', async () => {
    setBrowserLanguages('sv-SE')
    const user = userEvent.setup()
    const first = renderWithI18n(<App />)

    await user.selectOptions(screen.getByRole('combobox'), 'en')

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Chores')
    expect(localStorage.getItem(languageStorageKey)).toBe('en')

    first.unmount()
    renderWithI18n(<App />)

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Chores')
  })
})
