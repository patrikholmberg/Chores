import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { ApiError } from '../api/ApiError'
import { fakeApi } from '../test/fakeApi'
import { fakeAuth, parent, renderApp } from '../test/renderApp'
import type { Member } from './members'

const nova: Member = { id: 'c-1', username: 'nova', displayName: 'Nova', role: 'child', preferredLanguage: 'sv', email: null }

describe('Member management', () => {
  it('lists the family members', async () => {
    const api = fakeApi({ 'GET /api/members': [nova] })
    renderApp({ route: '/members', auth: fakeAuth(parent, { api }) })

    expect(await screen.findByRole('link', { name: /Nova/ })).toHaveAttribute('href', '/members/c-1')
  })

  it('adds a child', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'GET /api/members': [] })
    renderApp({ route: '/members/new', auth: fakeAuth(parent, { api }) })

    await user.type(screen.getByLabelText('Username'), 'nova')
    await user.type(screen.getByLabelText('Password'), 'abcd')
    await user.click(screen.getByRole('button', { name: 'Add member' }))

    expect(api).toHaveBeenCalledWith('/api/members', {
      method: 'POST',
      body: expect.objectContaining({ username: 'nova', role: 'child', email: null, password: 'abcd' }),
    })
    expect(await screen.findByRole('heading', { name: 'Members' })).toBeInTheDocument()
  })

  it('asks for an email only for a parent', async () => {
    const user = userEvent.setup()
    renderApp({ route: '/members/new', auth: fakeAuth(parent) })

    expect(screen.queryByLabelText('Email')).not.toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Role'), 'parent')

    expect(screen.getByLabelText('Email')).toBeInTheDocument()
    expect(screen.getByText('At least 10 characters.')).toBeInTheDocument()
  })

  it('confirms removal in a modal before removing', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'GET /api/members/c-1': nova, 'GET /api/members': [] })
    renderApp({ route: '/members/c-1', auth: fakeAuth(parent, { api }) })

    await user.click(await screen.findByRole('button', { name: 'Remove member' }))

    const dialog = await screen.findByRole('dialog')
    await waitFor(() => expect(dialog).toHaveClass('show'))
    expect(dialog).toHaveTextContent('Remove Nova?')
    expect(api).not.toHaveBeenCalledWith('/api/members/c-1', { method: 'DELETE' })

    await user.click(screen.getAllByRole('button', { name: 'Remove member' }).at(-1)!)

    expect(api).toHaveBeenCalledWith('/api/members/c-1', { method: 'DELETE' })
  })

  it('explains why the last parent cannot be demoted', async () => {
    const user = userEvent.setup()
    const me = { ...nova, id: 'p-1', username: 'alex', displayName: 'Alex', role: 'parent' as const, email: 'alex@example.com' }
    const api = fakeApi({
      'GET /api/members/p-1': me,
      'PUT /api/members/p-1': () => {
        throw new ApiError(409, 'A Family must keep at least one Parent.', 'member.last_parent')
      },
    })
    renderApp({ route: '/members/p-1', auth: fakeAuth(parent, { api }) })

    await user.selectOptions(await screen.findByLabelText('Role'), 'child')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('A family must keep at least one parent.')
  })
})
