import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { child, fakeAuth, renderApp } from '../test/renderApp'

describe('HomePage', () => {
  it('greets the member and names their family', () => {
    renderApp({ auth: fakeAuth(child) })

    expect(screen.getByRole('heading', { name: 'Hi Nova!' })).toBeInTheDocument()
    expect(screen.getByText('Family: The Holmbergs')).toBeInTheDocument()
  })
})
