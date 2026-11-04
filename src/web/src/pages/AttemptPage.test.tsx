import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { ApiError } from '../api/ApiError'
import { fakeApi } from '../test/fakeApi'
import { child, fakeAuth, parent, renderApp } from '../test/renderApp'
import type { Attempt } from './attempts'
import type { Chore } from './chores'

const inAWeek = new Date(Date.now() + 7 * 24 * 3600 * 1000).toISOString()

const washClothes: Chore = {
  id: 'ch-1',
  title: 'Wash clothes',
  description: null,
  pointMultiplier: 1,
  revision: 0,
  archived: false,
  steps: [{ title: 'Wash clothes', description: null, reminderDelayMinutes: null, repeatIntervalMinutes: null }],
}

const open: Attempt = {
  id: 'a-1',
  childId: child.memberId,
  childName: 'Nova',
  choreId: 'ch-1',
  title: 'Wash clothes',
  description: null,
  pointMultiplier: 1,
  startDay: '2026-11-02',
  startedAt: '2026-11-02T17:00:00Z',
  expiresAt: inAWeek,
  completedAt: null,
  state: 'open',
  currentStepPosition: 0,
  rejection: null,
  revision: 0,
  steps: [
    { position: 0, title: 'Wash clothes', description: null, reminderDelayMinutes: null, repeatIntervalMinutes: null, completedAt: null },
    { position: 1, title: 'Hang clothes', description: null, reminderDelayMinutes: 90, repeatIntervalMinutes: 30, completedAt: null },
  ],
}

const afterFirst: Attempt = {
  ...open,
  currentStepPosition: 1,
  revision: 1,
  steps: [{ ...open.steps[0], completedAt: '2026-11-02T17:30:00Z' }, open.steps[1]],
}

const complete: Attempt = {
  ...open,
  state: 'complete',
  currentStepPosition: null,
  completedAt: '2026-11-02T18:00:00Z',
  steps: open.steps.map((s) => ({ ...s, completedAt: '2026-11-02T18:00:00Z' })),
}

const steps = () => within(screen.getByRole('list', { name: 'Steps' })).getAllByRole('listitem')

async function confirmIn(user: ReturnType<typeof userEvent.setup>, title: string, confirm: string) {
  const dialog = await screen.findByRole('dialog')
  await waitFor(() => expect(dialog).toHaveClass('show'))
  expect(dialog).toHaveTextContent(title)
  await user.click(within(dialog).getByRole('button', { name: confirm }))
}

describe('Chore Attempts', () => {
  it('starts an attempt from a chore and opens it', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'GET /api/chores/ch-1': washClothes, 'POST /api/attempts': open, 'GET /api/attempts/a-1': open })
    renderApp({ route: '/chores/ch-1', auth: fakeAuth(child, { api }) })

    await user.click(await screen.findByRole('button', { name: 'Start' }))

    expect(api).toHaveBeenCalledWith('/api/attempts', { method: 'POST', body: { choreId: 'ch-1' } })
    expect(await screen.findByText('In progress')).toBeInTheDocument()
    expect(screen.getByRole('list', { name: 'Steps' })).toBeInTheDocument()
  })

  it('offers no Start button to a parent', async () => {
    renderApp({ route: '/chores/ch-1', auth: fakeAuth(parent, { api: fakeApi({ 'GET /api/chores/ch-1': washClothes }) }) })

    expect(await screen.findByRole('heading', { name: 'Wash clothes' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Start' })).not.toBeInTheDocument()
  })

  it('completes the current step and moves to the next', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'GET /api/attempts/a-1': open, 'POST /api/attempts/a-1/steps/0/complete': afterFirst })
    renderApp({ route: '/attempts/a-1', auth: fakeAuth(child, { api }) })

    await screen.findByRole('list', { name: 'Steps' })
    expect(steps()[0]).toHaveAttribute('aria-current', 'step')
    expect(screen.getAllByRole('button', { name: 'Done' })).toHaveLength(1)

    await user.click(within(steps()[0]).getByRole('button', { name: 'Done' }))

    expect(api).toHaveBeenCalledWith('/api/attempts/a-1/steps/0/complete', { method: 'POST' })
    await waitFor(() => expect(steps()[1]).toHaveAttribute('aria-current', 'step'))
    expect(steps()[0]).toHaveTextContent('Done')
    expect(within(steps()[1]).getByRole('button', { name: 'Done' })).toBeInTheDocument()
  })

  it('starts over after confirmation', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'GET /api/attempts/a-1': afterFirst, 'POST /api/attempts/a-1/restart': open })
    renderApp({ route: '/attempts/a-1', auth: fakeAuth(child, { api }) })

    await user.click(await screen.findByRole('button', { name: 'Start over' }))
    expect(api).not.toHaveBeenCalledWith('/api/attempts/a-1/restart', { method: 'POST' })
    await confirmIn(user, 'Start Wash clothes over?', 'Start over')

    expect(api).toHaveBeenCalledWith('/api/attempts/a-1/restart', { method: 'POST' })
    await waitFor(() => expect(steps()[0]).toHaveAttribute('aria-current', 'step'))
  })

  it('cancels after confirmation and goes home', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'GET /api/attempts/a-1': open })
    renderApp({ route: '/attempts/a-1', auth: fakeAuth(child, { api }) })

    await user.click(await screen.findByRole('button', { name: 'Cancel chore' }))
    await confirmIn(user, 'Cancel Wash clothes?', 'Cancel chore')

    expect(api).toHaveBeenCalledWith('/api/attempts/a-1', { method: 'DELETE' })
    expect(await screen.findByRole('heading', { name: 'Hi Nova!' })).toBeInTheDocument()
  })

  it('lets a parent reject a complete attempt with a reason, but not tick off steps', async () => {
    const user = userEvent.setup()
    const rejected: Attempt = {
      ...complete,
      state: 'rejected',
      rejection: { at: '2026-11-02T19:00:00Z', byMemberId: parent.memberId, byName: 'Alex', reason: 'Not clean' },
    }
    const api = fakeApi({ 'GET /api/attempts/a-1': complete, 'POST /api/attempts/a-1/reject': rejected })
    renderApp({ route: '/attempts/a-1', auth: fakeAuth(parent, { api }) })

    await user.click(await screen.findByRole('button', { name: 'Reject' }))
    expect(screen.queryByRole('button', { name: 'Done' })).not.toBeInTheDocument()
    const dialog = await screen.findByRole('dialog')
    await waitFor(() => expect(dialog).toHaveClass('show'))
    await user.type(within(dialog).getByLabelText('Reason'), 'Not clean')
    await user.click(within(dialog).getByRole('button', { name: 'Reject' }))

    expect(api).toHaveBeenCalledWith('/api/attempts/a-1/reject', { method: 'POST', body: { reason: 'Not clean' } })
    expect(await screen.findByText('Rejected by Alex.')).toBeInTheDocument()
    expect(screen.getByText('Reason: Not clean')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Reject' })).not.toBeInTheDocument()
  })

  it('shows a parent no Done button on an open attempt', async () => {
    renderApp({ route: '/attempts/a-1', auth: fakeAuth(parent, { api: fakeApi({ 'GET /api/attempts/a-1': open }) }) })

    await screen.findByRole('list', { name: 'Steps' })
    expect(screen.queryByRole('button', { name: 'Done' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Reject' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Cancel chore' })).toBeInTheDocument()
  })

  it('reloads and explains a conflict', async () => {
    const user = userEvent.setup()
    let loads = 0
    const api = fakeApi({
      'GET /api/attempts/a-1': () => (++loads === 1 ? open : afterFirst),
      'POST /api/attempts/a-1/steps/0/complete': () => {
        throw new ApiError(409, 'Conflict', 'attempt.not_current_step')
      },
    })
    renderApp({ route: '/attempts/a-1', auth: fakeAuth(child, { api }) })

    await user.click(await screen.findByRole('button', { name: 'Done' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('That step is not the current one.')
    await waitFor(() => expect(steps()[1]).toHaveAttribute('aria-current', 'step'))
  })
})
