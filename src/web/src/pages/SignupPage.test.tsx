import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/ApiError'
import { fakeApi } from '../test/fakeApi'
import { fakeAuth, renderApp } from '../test/renderApp'

vi.mock('../forms/CaptchaField', () => ({
  CaptchaField: ({ error }: { error?: string }) => (
    <>
      <input type="hidden" name="altcha" value="solved-captcha" />
      {error && <div>{error}</div>}
    </>
  ),
}))

function deviceTimeZone(zone: string) {
  const original = Intl.DateTimeFormat.prototype.resolvedOptions
  vi.spyOn(Intl.DateTimeFormat.prototype, 'resolvedOptions').mockImplementation(function (this: Intl.DateTimeFormat) {
    return { ...original.call(this), timeZone: zone }
  })
}

async function fillIn(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('Family name'), 'The Holmbergs')
  await user.type(screen.getByLabelText('Username'), 'alex')
  await user.type(screen.getByLabelText('Password'), 'long-enough-pw')
  await user.type(screen.getByLabelText('Email'), 'alex@example.com')
}

// Typing a username triggers the debounced availability check, which every test that fills in the form must answer.
const available = { 'GET /api/usernames/alex/availability': { available: true } }

describe('SignupPage', () => {
  afterEach(() => vi.restoreAllMocks())

  it('pre-fills the time zone from the device', () => {
    deviceTimeZone('Europe/Stockholm')
    renderApp({ route: '/signup' })

    expect(screen.getByLabelText('Time zone')).toHaveValue('Europe/Stockholm')
  })

  it('says when the username is taken', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'GET /api/usernames/alex/availability': { available: false } })
    renderApp({ route: '/signup', auth: fakeAuth(undefined, { api }) })

    await user.type(screen.getByLabelText('Username'), 'alex')

    await waitFor(() => expect(screen.getByText('That username is already taken.')).toBeInTheDocument(), { timeout: 2000 })
    expect(screen.getByLabelText('Username')).toHaveClass('is-invalid')
  })

  it('shows server validation errors on the right fields', async () => {
    const user = userEvent.setup()
    const api = fakeApi({
      ...available,
      'POST /api/families': () => {
        throw new ApiError(400, 'One or more validation errors occurred.', undefined, {
          Email: ['email.invalid'],
          Password: ['password.too_short'],
        })
      },
    })
    renderApp({ route: '/signup', auth: fakeAuth(undefined, { api }) })

    await fillIn(user)
    await user.click(screen.getByRole('button', { name: 'Create family' }))

    expect(await screen.findByText('Enter a valid email address.')).toBeInTheDocument()
    expect(screen.getByText('The password must be at least 10 characters.')).toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toHaveClass('is-invalid')
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('creates the family with the CAPTCHA solution and signs the parent in', async () => {
    const user = userEvent.setup()
    const api = fakeApi(available)
    const { auth } = renderApp({ route: '/signup', auth: fakeAuth(undefined, { api }) })

    await fillIn(user)
    await user.click(screen.getByRole('button', { name: 'Create family' }))

    await waitFor(() => expect(auth.signIn).toHaveBeenCalledWith('/'))
    expect(api).toHaveBeenCalledWith('/api/families', {
      method: 'POST',
      body: expect.objectContaining({ familyName: 'The Holmbergs', username: 'alex', captcha: 'solved-captcha' }),
    })
    expect(api).toHaveBeenCalledWith('/api/account/login', {
      method: 'POST',
      body: { username: 'alex', password: 'long-enough-pw' },
    })
  })
})
