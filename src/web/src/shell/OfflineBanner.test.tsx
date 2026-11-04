import { act, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderWithI18n } from '../test/renderWithI18n'
import { OfflineBanner } from './OfflineBanner'

function setOnline(value: boolean) {
  vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(value)
}

describe('OfflineBanner', () => {
  afterEach(() => vi.restoreAllMocks())

  it('is hidden while online', () => {
    setOnline(true)
    renderWithI18n(<OfflineBanner />)
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('appears when the device goes offline', () => {
    setOnline(true)
    renderWithI18n(<OfflineBanner />)

    setOnline(false)
    act(() => {
      window.dispatchEvent(new Event('offline'))
    })

    expect(screen.getByRole('status')).toBeInTheDocument()
  })
})
