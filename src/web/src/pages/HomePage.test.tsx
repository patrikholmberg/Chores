import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { fakeApi } from '../test/fakeApi'
import { child, fakeAuth, parent, renderApp } from '../test/renderApp'
import type { AttemptSummary, TodayPoints } from './attempts'

const inAWeek = new Date(Date.now() + 7 * 24 * 3600 * 1000).toISOString()

function summary(overrides: Partial<AttemptSummary>): AttemptSummary {
  return {
    id: 'a-1',
    childId: child.memberId,
    childName: 'Nova',
    choreId: 'ch-1',
    title: 'Wash clothes',
    pointMultiplier: 1,
    startDay: '2026-11-02',
    startedAt: '2026-11-02T17:00:00Z',
    expiresAt: inAWeek,
    completedAt: null,
    state: 'open',
    stepCount: 3,
    currentStepPosition: 1,
    currentStepTitle: 'Hang clothes',
    rejection: null,
    ...overrides,
  }
}

describe('HomePage', () => {
  it('greets the member and names their family', () => {
    renderApp({ auth: fakeAuth(child) })

    expect(screen.getByRole('heading', { name: 'Hi Nova!' })).toBeInTheDocument()
    expect(screen.getByText('Family: The Holmbergs')).toBeInTheDocument()
  })

  it("shows a child today's points, open attempts and today's finished ones", async () => {
    const today: TodayPoints = { day: '2026-11-02', basePoints: 10, children: [{ childId: child.memberId, childName: 'Nova', points: 30 }] }
    const api = fakeApi({
      'GET /api/points/today': today,
      'GET /api/attempts': [
        summary({ id: 'a-1' }),
        summary({ id: 'a-2', title: 'Empty the dishwasher', state: 'complete', currentStepPosition: null, currentStepTitle: null }),
        summary({ id: 'a-3', title: 'Clean the kitchen', state: 'rejected', currentStepPosition: null, currentStepTitle: null }),
        summary({ id: 'a-4', title: 'Old laundry', startDay: '2026-11-01', currentStepTitle: 'Collect clothes' }),
      ],
    })
    renderApp({ auth: fakeAuth(child, { api }) })

    expect(await screen.findByText('30 points')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Do a chore' })).toHaveAttribute('href', '/chores')

    const inProgress = within(screen.getByLabelText('In progress')).getAllByRole('link')
    expect(inProgress.map((l) => l.getAttribute('href'))).toEqual(['/attempts/a-1', '/attempts/a-4'])
    expect(inProgress[0]).toHaveTextContent('Now: Hang clothes')
    expect(inProgress[1]).toHaveTextContent('Started Sun 1 Nov')

    const done = within(screen.getByLabelText('Finished today')).getAllByRole('link')
    expect(done.map((l) => l.textContent)).toEqual(['Empty the dishwasherComplete', 'Clean the kitchenRejected'])
  })

  it("shows a parent each child's points and recent attempts with Reject where allowed", async () => {
    const today: TodayPoints = {
      day: '2026-11-02',
      basePoints: 10,
      children: [
        { childId: 'c-2', childName: 'Liam', points: 0 },
        { childId: child.memberId, childName: 'Nova', points: 20 },
      ],
    }
    const api = fakeApi({
      'GET /api/points/today': today,
      'GET /api/attempts': [
        summary({ id: 'a-1', state: 'complete' }),
        summary({ id: 'a-2', state: 'complete', expiresAt: '2020-01-01T00:00:00Z' }),
        summary({ id: 'a-3', childId: 'c-2', childName: 'Liam' }),
      ],
    })
    renderApp({ auth: fakeAuth(parent, { api }) })

    const card = await screen.findByLabelText('Today')
    expect(within(card).getAllByRole('listitem').map((li) => li.textContent)).toEqual(['Liam0 points', 'Nova20 points'])

    const recent = within(screen.getByLabelText('Recent chores')).getAllByRole('listitem')
    expect(recent).toHaveLength(3)
    expect(within(recent[0]).getByRole('link', { name: 'Reject' })).toHaveAttribute('href', '/attempts/a-1')
    expect(within(recent[1]).queryByRole('link', { name: 'Reject' })).not.toBeInTheDocument()
    expect(recent[2]).toHaveTextContent('Liam · Mon 2 Nov')
    expect(recent[2]).toHaveTextContent('In progress')
  })
})
