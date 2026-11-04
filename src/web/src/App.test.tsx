import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languageStorageKey } from './i18n'
import { renderApp } from './test/renderApp'

const loginRoute = '/login?returnUrl=%2Fconnect%2Fauthorize%3Fclient_id%3Dchores-web'

function setBrowserLanguages(...languages: string[]) {
  vi.spyOn(navigator, 'languages', 'get').mockReturnValue(languages)
  vi.spyOn(navigator, 'language', 'get').mockReturnValue(languages[0])
}

describe('App language', () => {
  beforeEach(() => localStorage.clear())
  afterEach(() => vi.restoreAllMocks())

  it('uses Swedish on first visit when the browser prefers Swedish', () => {
    setBrowserLanguages('sv-SE')
    renderApp({ route: loginRoute })

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Sysslor')
    expect(document.documentElement.lang).toBe('sv')
  })

  it('falls back to English for an unsupported browser language', () => {
    setBrowserLanguages('de-DE')
    renderApp({ route: loginRoute })

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Chores')
  })

  it('switches immediately and remembers the choice after a reload', async () => {
    setBrowserLanguages('sv-SE')
    const user = userEvent.setup()
    const first = renderApp({ route: loginRoute })

    await user.selectOptions(screen.getByRole('combobox', { name: 'Språk' }), 'en')

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Chores')
    expect(localStorage.getItem(languageStorageKey)).toBe('en')

    first.unmount()
    renderApp({ route: loginRoute })

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Chores')
  })
})
