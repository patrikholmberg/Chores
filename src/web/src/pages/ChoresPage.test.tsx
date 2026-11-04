import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { fakeApi } from '../test/fakeApi'
import { child, fakeAuth, parent, renderApp } from '../test/renderApp'
import type { Chore, ChoreSummary } from './chores'

const summary: ChoreSummary = { id: 'ch-1', title: 'Wash clothes', description: null, pointMultiplier: 2, stepCount: 3 }

const washClothes: Chore = {
  id: 'ch-1',
  title: 'Wash clothes',
  description: 'All of it',
  pointMultiplier: 2,
  revision: 0,
  archived: false,
  steps: [
    { title: 'Wash clothes', description: null, reminderDelayMinutes: null, repeatIntervalMinutes: null },
    { title: 'Hang clothes', description: null, reminderDelayMinutes: 90, repeatIntervalMinutes: 30 },
    { title: 'Collect clothes', description: null, reminderDelayMinutes: 2880, repeatIntervalMinutes: null },
  ],
}

describe('Chore Library', () => {
  it('lists the chores for a child without parent actions', async () => {
    const api = fakeApi({ 'GET /api/chores': [summary] })
    renderApp({ route: '/chores', auth: fakeAuth(child, { api }) })

    const link = await screen.findByRole('link', { name: /Wash clothes/ })
    expect(link).toHaveAttribute('href', '/chores/ch-1')
    expect(link).toHaveTextContent('3 steps')
    expect(within(link).getByText('×2')).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'New chore' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Archived' })).not.toBeInTheDocument()
  })

  it('offers a parent to add chores and open the archive', async () => {
    const api = fakeApi({ 'GET /api/chores': [] })
    renderApp({ route: '/chores', auth: fakeAuth(parent, { api }) })

    expect(await screen.findByText('There are no chores yet. Add the first one!')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'New chore' })).toHaveAttribute('href', '/chores/new')
    expect(screen.getByRole('link', { name: 'Archived' })).toHaveAttribute('href', '/chores/archived')
  })

  it('shows a chore with its steps and reminders to a child, without parent actions', async () => {
    const api = fakeApi({ 'GET /api/chores/ch-1': washClothes })
    renderApp({ route: '/chores/ch-1', auth: fakeAuth(child, { api }) })

    expect(await screen.findByRole('heading', { name: 'Wash clothes' })).toBeInTheDocument()
    const steps = within(screen.getByRole('list', { name: 'Steps' })).getAllByRole('listitem')
    expect(steps.map((s) => s.querySelector('.fw-semibold')?.textContent)).toEqual(['Wash clothes', 'Hang clothes', 'Collect clothes'])
    expect(steps[1]).toHaveTextContent('Reminder 1 h 30 min after the previous step, repeats every 30 min')
    expect(steps[2]).toHaveTextContent('Reminder 2 d after the previous step')
    expect(screen.queryByRole('link', { name: 'Edit' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Archive' })).not.toBeInTheDocument()
  })

  it('archives a chore after confirmation and returns to the library', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'GET /api/chores/ch-1': washClothes, 'GET /api/chores': [] })
    renderApp({ route: '/chores/ch-1', auth: fakeAuth(parent, { api }) })

    await user.click(await screen.findByRole('button', { name: 'Archive' }))

    const dialog = await screen.findByRole('dialog')
    await waitFor(() => expect(dialog).toHaveClass('show'))
    expect(dialog).toHaveTextContent('Archive Wash clothes?')
    expect(api).not.toHaveBeenCalledWith('/api/chores/ch-1', { method: 'DELETE' })

    await user.click(within(dialog).getByRole('button', { name: 'Archive' }))

    expect(api).toHaveBeenCalledWith('/api/chores/ch-1', { method: 'DELETE' })
    expect(await screen.findByRole('heading', { name: 'Chores', level: 2 })).toBeInTheDocument()
  })
})
