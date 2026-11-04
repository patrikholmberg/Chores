import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { createI18n } from '../i18n'
import { child, fakeAuth, renderApp } from '../test/renderApp'
import { formatDuration } from './chores'

describe('formatDuration', () => {
  it.each([
    ['en', 90, '1 h 30 min'],
    ['en', 2880, '2 d'],
    ['en', 1530, '1 d 1 h 30 min'],
    ['sv', 90, '1 tim 30 min'],
    ['sv', 2880, '2 d'],
    ['sv', 15, '15 min'],
  ])('in %s renders %i minutes as "%s"', async (language, minutes, expected) => {
    const i18n = createI18n()
    await i18n.changeLanguage(language)

    expect(formatDuration(i18n.t, minutes)).toBe(expected)
  })
})

describe('Chore Library navigation', () => {
  it('shows the Chores item to a child', () => {
    renderApp({ auth: fakeAuth(child) })

    expect(screen.getByRole('link', { name: 'Chores' })).toHaveAttribute('href', '/chores')
    expect(screen.queryByRole('link', { name: 'Members' })).not.toBeInTheDocument()
  })

  it('keeps a child out of the chore form', () => {
    renderApp({ route: '/chores/new', auth: fakeAuth(child) })

    expect(screen.getByRole('heading', { name: 'Hi Nova!' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'New chore' })).not.toBeInTheDocument()
  })
})
