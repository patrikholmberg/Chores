import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { ApiError } from '../api/ApiError'
import { fakeApi } from '../test/fakeApi'
import { fakeAuth, parent, renderApp } from '../test/renderApp'
import type { Chore } from './chores'

const washClothes: Chore = {
  id: 'ch-1',
  title: 'Wash clothes',
  description: null,
  pointMultiplier: 1,
  revision: 4,
  archived: false,
  steps: [
    { title: 'Wash clothes', description: null, reminderDelayMinutes: null, repeatIntervalMinutes: null },
    { title: 'Hang clothes', description: null, reminderDelayMinutes: 90, repeatIntervalMinutes: 30 },
    { title: 'Collect clothes', description: null, reminderDelayMinutes: 2880, repeatIntervalMinutes: null },
  ],
}

const step = (number: number) => screen.getByRole('group', { name: `Step ${number}` })

describe('Chore form', () => {
  it('creates a chore with three steps and reminders', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'POST /api/chores': washClothes, 'GET /api/chores/ch-1': washClothes })
    renderApp({ route: '/chores/new', auth: fakeAuth(parent, { api }) })

    await user.type(screen.getByLabelText('Title'), 'Wash clothes')
    await user.type(within(step(1)).getByLabelText('What to do'), 'Wash clothes')
    expect(within(step(1)).queryByText('Reminder after the previous step')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Add step' }))
    await user.type(within(step(2)).getByLabelText('What to do'), 'Hang clothes')
    expect(within(step(2)).getByLabelText('Repeat the reminder every: hours')).toBeDisabled()
    await user.type(within(step(2)).getByLabelText('Reminder after the previous step: hours'), '1')
    await user.type(within(step(2)).getByLabelText('Reminder after the previous step: minutes'), '30')
    await user.type(within(step(2)).getByLabelText('Repeat the reminder every: minutes'), '30')

    await user.click(screen.getByRole('button', { name: 'Add step' }))
    await user.type(within(step(3)).getByLabelText('What to do'), 'Collect clothes')
    await user.type(within(step(3)).getByLabelText('Reminder after the previous step: hours'), '48')

    await user.click(screen.getByRole('button', { name: 'Create chore' }))

    expect(api).toHaveBeenCalledWith('/api/chores', {
      method: 'POST',
      body: {
        title: 'Wash clothes',
        description: null,
        pointMultiplier: 1,
        steps: [
          { title: 'Wash clothes', description: null, reminderDelayMinutes: null, repeatIntervalMinutes: null },
          { title: 'Hang clothes', description: null, reminderDelayMinutes: 90, repeatIntervalMinutes: 30 },
          { title: 'Collect clothes', description: null, reminderDelayMinutes: 2880, repeatIntervalMinutes: null },
        ],
      },
    })
    expect(await screen.findByRole('heading', { name: 'Wash clothes' })).toBeInTheDocument()
  })

  it('reorders steps and sends the revision it loaded', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'GET /api/chores/ch-1': washClothes, 'PUT /api/chores/ch-1': washClothes })
    renderApp({ route: '/chores/ch-1/edit', auth: fakeAuth(parent, { api }) })

    await user.click(await screen.findByRole('button', { name: 'Move step 3 up' }))
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(api).toHaveBeenCalledWith('/api/chores/ch-1', {
      method: 'PUT',
      body: expect.objectContaining({
        revision: 4,
        steps: [
          expect.objectContaining({ title: 'Wash clothes' }),
          expect.objectContaining({ title: 'Collect clothes', reminderDelayMinutes: 2880 }),
          expect.objectContaining({ title: 'Hang clothes', reminderDelayMinutes: 90, repeatIntervalMinutes: 30 }),
        ],
      }),
    })
  })

  it('removes the reminder of a step moved first and says so', async () => {
    const user = userEvent.setup()
    const api = fakeApi({ 'GET /api/chores/ch-1': washClothes, 'PUT /api/chores/ch-1': washClothes })
    renderApp({ route: '/chores/ch-1/edit', auth: fakeAuth(parent, { api }) })

    await user.click(await screen.findByRole('button', { name: 'Move step 2 up' }))

    expect(screen.getByRole('status')).toHaveTextContent('Hang clothes is now the first step, so its reminder was removed.')
    expect(within(step(1)).getByLabelText('What to do')).toHaveValue('Hang clothes')

    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(api).toHaveBeenCalledWith('/api/chores/ch-1', {
      method: 'PUT',
      body: expect.objectContaining({
        steps: [
          expect.objectContaining({ title: 'Hang clothes', reminderDelayMinutes: null, repeatIntervalMinutes: null }),
          expect.objectContaining({ title: 'Wash clothes' }),
          expect.objectContaining({ title: 'Collect clothes' }),
        ],
      }),
    })
  })

  it('shows a server error on the step field it belongs to', async () => {
    const user = userEvent.setup()
    const api = fakeApi({
      'GET /api/chores/ch-1': washClothes,
      'PUT /api/chores/ch-1': () => {
        throw new ApiError(400, 'Invalid', undefined, { 'Steps[1].RepeatIntervalMinutes': ['step.interval_out_of_range'] })
      },
    })
    renderApp({ route: '/chores/ch-1/edit', auth: fakeAuth(parent, { api }) })

    await user.click(await screen.findByRole('button', { name: 'Save' }))

    expect(await within(step(2)).findByText('Use 15 minutes to 7 days.')).toBeInTheDocument()
    expect(within(step(3)).queryByText('Use 15 minutes to 7 days.')).not.toBeInTheDocument()
  })

  it('explains a conflict and reloads the saved chore', async () => {
    const user = userEvent.setup()
    let saved = washClothes
    const api = fakeApi({
      'GET /api/chores/ch-1': () => saved,
      'PUT /api/chores/ch-1': () => {
        throw new ApiError(409, 'Changed', 'concurrency_conflict')
      },
    })
    renderApp({ route: '/chores/ch-1/edit', auth: fakeAuth(parent, { api }) })

    await user.clear(await screen.findByLabelText('Title'))
    await user.type(screen.getByLabelText('Title'), 'Mine')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Someone else changed this at the same time.')

    saved = { ...washClothes, title: 'Laundry', revision: 5 }
    await user.click(screen.getByRole('button', { name: 'Reload' }))

    expect(await screen.findByDisplayValue('Laundry')).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
