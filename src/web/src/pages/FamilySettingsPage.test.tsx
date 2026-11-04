import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { ApiError } from '../api/ApiError'
import { fakeApi } from '../test/fakeApi'
import { child, fakeAuth, parent, renderApp } from '../test/renderApp'

describe('Family settings', () => {
  it('saves base points and the attempt window', async () => {
    const user = userEvent.setup()
    const api = fakeApi({
      'GET /api/family/settings': { basePoints: 10, attemptWindowDays: 7 },
      'PUT /api/family/settings': (body: unknown) => body,
    })
    renderApp({ route: '/settings', auth: fakeAuth(parent, { api }) })

    const basePoints = await screen.findByLabelText('Base points')
    expect(basePoints).toHaveValue(10)
    expect(screen.getByLabelText('Days to finish a chore')).toHaveValue(7)

    await user.clear(basePoints)
    await user.type(basePoints, '5')
    await user.clear(screen.getByLabelText('Days to finish a chore'))
    await user.type(screen.getByLabelText('Days to finish a chore'), '3')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(api).toHaveBeenCalledWith('/api/family/settings', { method: 'PUT', body: { basePoints: 5, attemptWindowDays: 3 } })
    expect(await screen.findByText('The settings were saved.')).toBeInTheDocument()
  })

  it('shows a server field error on its input', async () => {
    const user = userEvent.setup()
    const api = fakeApi({
      'GET /api/family/settings': { basePoints: 10, attemptWindowDays: 7 },
      'PUT /api/family/settings': () => {
        throw new ApiError(400, 'Invalid', undefined, { BasePoints: ['family.base_points_out_of_range'] })
      },
    })
    renderApp({ route: '/settings', auth: fakeAuth(parent, { api }) })

    await user.clear(await screen.findByLabelText('Base points'))
    await user.type(screen.getByLabelText('Base points'), '0')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Use a whole number from 1 to 1000.')).toBeInTheDocument()
    expect(screen.getByLabelText('Base points')).toHaveClass('is-invalid')
    expect(screen.getByLabelText('Days to finish a chore')).not.toHaveClass('is-invalid')
  })

  it('shows the Settings item to a parent only and keeps a child out', () => {
    const { unmount } = renderApp({ auth: fakeAuth(parent) })
    expect(screen.getByRole('link', { name: 'Settings' })).toHaveAttribute('href', '/settings')
    unmount()

    renderApp({ route: '/settings', auth: fakeAuth(child) })
    expect(screen.queryByRole('link', { name: 'Settings' })).not.toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Family settings' })).not.toBeInTheDocument()
  })
})
