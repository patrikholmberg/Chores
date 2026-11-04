import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { fakeApi } from '../test/fakeApi'
import { fakeAuth, parent, renderApp } from '../test/renderApp'
import type { ChoreSummary } from './chores'

const archived: ChoreSummary = { id: 'ch-1', title: 'Wash clothes', description: null, pointMultiplier: 1, stepCount: 3 }

describe('Archived chores', () => {
  it('restores a chore and removes it from the archived list', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'GET /api/chores/archived': [archived] })
    renderApp({ route: '/chores/archived', auth: fakeAuth(parent, { api }) })

    await user.click(await screen.findByRole('button', { name: 'Restore Wash clothes' }))

    expect(api).toHaveBeenCalledWith('/api/chores/ch-1/restore', { method: 'POST' })
    expect(await screen.findByRole('status')).toHaveTextContent('Wash clothes is back in the library.')
    expect(screen.getByText('No chores are archived.')).toBeInTheDocument()
  })
})
