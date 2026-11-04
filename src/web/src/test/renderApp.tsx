import { render } from '@testing-library/react'
import type { UserManager } from 'oidc-client-ts'
import type { ReactElement } from 'react'
import { I18nextProvider } from 'react-i18next'
import { MemoryRouter } from 'react-router'
import { vi } from 'vitest'
import { AppRoutes } from '../App'
import { AuthContext, type Auth, type CurrentMember } from '../auth/AuthContext'
import { createI18n } from '../i18n'

export const parent: CurrentMember = {
  memberId: 'p-1',
  username: 'alex',
  displayName: 'Alex',
  role: 'parent',
  preferredLanguage: 'en',
  familyId: 'f-1',
  familyName: 'The Holmbergs',
  timeZone: 'Europe/Stockholm',
}

export const child: CurrentMember = { ...parent, memberId: 'c-1', username: 'nova', displayName: 'Nova', role: 'child' }

/** An auth context with spies, signed out unless a Member is given. */
export function fakeAuth(member?: CurrentMember, overrides: Partial<Auth> = {}): Auth {
  return {
    status: member ? 'signedIn' : 'signedOut',
    member,
    api: vi.fn(async () => undefined) as unknown as Auth['api'],
    signIn: vi.fn(async () => {}),
    signOut: vi.fn(async () => {}),
    reloadMember: vi.fn(async () => {}),
    ...overrides,
  }
}

/** Renders with a fresh i18n instance, a fake auth context and an in-memory router at `route`. */
export function renderWithProviders(ui: ReactElement, { auth = fakeAuth(), route = '/' }: { auth?: Auth; route?: string } = {}) {
  const i18n = createI18n()
  return {
    i18n,
    auth,
    ...render(
      <I18nextProvider i18n={i18n}>
        <AuthContext.Provider value={auth}>
          <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
        </AuthContext.Provider>
      </I18nextProvider>,
    ),
  }
}

/** Renders the whole route table at `route`. */
export function renderApp(options: { auth?: Auth; route?: string } = {}) {
  return renderWithProviders(<AppRoutes userManager={{} as UserManager} />, options)
}
