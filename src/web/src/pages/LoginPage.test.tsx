import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/ApiError'
import { redirectTo } from '../shell/redirect'
import { fakeApi } from '../test/fakeApi'
import { fakeAuth, parent, renderApp } from '../test/renderApp'

vi.mock('../shell/redirect', () => ({ redirectTo: vi.fn() }))

const authorize = '/connect/authorize?client_id=chores-web&code_challenge=x'
const loginRoute = `/login?returnUrl=${encodeURIComponent(authorize)}`

describe('LoginPage', () => {
  beforeEach(() => vi.mocked(redirectTo).mockClear())

  it('checks the password and continues the authorization request', async () => {
    const user = userEvent.setup()
    const api = fakeApi({})
    renderApp({ route: loginRoute, auth: fakeAuth(undefined, { api }) })

    await user.type(screen.getByLabelText('Username'), 'Nova')
    await user.type(screen.getByLabelText('Password'), 'abcd')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(api).toHaveBeenCalledWith('/api/account/login', { method: 'POST', body: { username: 'Nova', password: 'abcd' } })
    expect(redirectTo).toHaveBeenCalledWith(authorize)
  })

  it('shows one message for any failed sign-in', async () => {
    const user = userEvent.setup()
    const api = fakeApi({
      'POST /api/account/login': () => {
        throw new ApiError(400, 'The username or password is incorrect.', 'signin.failed')
      },
    })
    renderApp({ route: loginRoute, auth: fakeAuth(undefined, { api }) })

    await user.type(screen.getByLabelText('Username'), 'nova')
    await user.type(screen.getByLabelText('Password'), 'nope')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('The username or password is incorrect.')
    expect(redirectTo).not.toHaveBeenCalled()
  })

  it('starts the authorization request when opened directly', () => {
    const { auth } = renderApp({ route: '/login' })

    expect(auth.signIn).toHaveBeenCalledWith('/')
  })

  it('never continues to anything but the authorize endpoint', () => {
    const { auth } = renderApp({ route: `/login?returnUrl=${encodeURIComponent('https://evil.example/')}` })

    expect(screen.queryByLabelText('Password')).not.toBeInTheDocument()
    expect(auth.signIn).toHaveBeenCalledWith('/')
  })

  it('signs out from the navigation bar', async () => {
    const user = userEvent.setup()
    const { auth } = renderApp({ auth: fakeAuth(parent) })

    await user.click(screen.getByRole('button', { name: 'Sign out' }))

    expect(auth.signOut).toHaveBeenCalled()
  })
})
