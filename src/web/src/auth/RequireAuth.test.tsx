import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { child, fakeAuth, parent, renderApp } from '../test/renderApp'

describe('route guards', () => {
  it('starts sign-in from a protected screen and keeps the target', () => {
    const { auth } = renderApp({ route: '/members?tab=all' })

    expect(auth.signIn).toHaveBeenCalledWith('/members?tab=all')
    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('does not start sign-in while the session is still loading', () => {
    const { auth } = renderApp({ route: '/members', auth: fakeAuth(undefined, { status: 'loading' }) })

    expect(auth.signIn).not.toHaveBeenCalled()
  })

  it('keeps Member management away from a child', () => {
    renderApp({ route: '/members', auth: fakeAuth(child) })

    expect(screen.queryByRole('link', { name: 'Members' })).not.toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Members' })).not.toBeInTheDocument()
  })

  it('shows Member management to a parent', () => {
    renderApp({ route: '/', auth: fakeAuth(parent) })

    expect(screen.getByRole('link', { name: 'Members' })).toBeInTheDocument()
  })
})
