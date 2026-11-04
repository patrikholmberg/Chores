import { afterEach, describe, expect, it, vi } from 'vitest'
import { followSystemColorScheme } from './colorScheme'

function mockMatchMedia(initiallyDark: boolean) {
  const listeners: Array<() => void> = []
  const query = {
    matches: initiallyDark,
    addEventListener: (_: string, listener: () => void) => listeners.push(listener),
    removeEventListener: vi.fn(),
  }
  vi.stubGlobal('matchMedia', () => query)
  return {
    setDark(value: boolean) {
      query.matches = value
      listeners.forEach((listener) => listener())
    },
  }
}

describe('followSystemColorScheme', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('applies the system scheme and follows changes', () => {
    const media = mockMatchMedia(true)
    const root = document.createElement('html')

    followSystemColorScheme(root)
    expect(root.getAttribute('data-bs-theme')).toBe('dark')

    media.setDark(false)
    expect(root.getAttribute('data-bs-theme')).toBe('light')
  })

  it('falls back to light without matchMedia', () => {
    vi.stubGlobal('matchMedia', undefined)
    const root = document.createElement('html')

    followSystemColorScheme(root)

    expect(root.getAttribute('data-bs-theme')).toBe('light')
  })
})
