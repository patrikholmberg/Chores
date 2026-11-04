import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { fakeAuth, parent, renderApp } from '../test/renderApp'

const loginRoute = '/login?returnUrl=%2Fconnect%2Fauthorize%3Fclient_id%3Dchores-web'

function setBrowserLanguages(...languages: string[]) {
  vi.spyOn(navigator, 'languages', 'get').mockReturnValue(languages)
  vi.spyOn(navigator, 'language', 'get').mockReturnValue(languages[0])
}

describe('signed-in Member language', () => {
  beforeEach(() => localStorage.clear())
  afterEach(() => vi.restoreAllMocks())

  it('shows a Swedish Member the UI in Swedish on an English browser', async () => {
    setBrowserLanguages('en-US')

    renderApp({ auth: fakeAuth({ ...parent, preferredLanguage: 'sv' }) })

    await waitFor(() => expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Sysslor'))
  })

  it('saves the language on the Member when switched while signed in', async () => {
    setBrowserLanguages('en-US')
    const user = userEvent.setup()
    const { auth } = renderApp({ auth: fakeAuth(parent) })

    await user.selectOptions(screen.getByRole('combobox', { name: 'Language' }), 'sv')

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Sysslor')
    expect(auth.api).toHaveBeenCalledWith('/api/me/language', { method: 'PUT', body: { language: 'sv' } })
    expect(auth.reloadMember).toHaveBeenCalled()
  })

  it('does not call the Api when switched while signed out', async () => {
    setBrowserLanguages('en-US')
    const user = userEvent.setup()
    const { auth } = renderApp({ route: loginRoute })

    await user.selectOptions(screen.getByRole('combobox', { name: 'Language' }), 'sv')

    expect(auth.api).not.toHaveBeenCalled()
  })
})
