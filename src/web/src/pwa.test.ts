import { describe, expect, it } from 'vitest'
import { manifest } from '../pwa.config'

describe('PWA manifest', () => {
  it('has the fields browsers require for installation', () => {
    expect(manifest).toBeTruthy()
    if (!manifest) return

    expect(manifest.name).toBeTruthy()
    expect(manifest.short_name).toBeTruthy()
    expect(manifest.start_url).toBe('/')
    expect(manifest.display).toBe('standalone')

    const sizes = manifest.icons?.map((icon) => icon.sizes) ?? []
    expect(sizes).toContain('192x192')
    expect(sizes).toContain('512x512')
    expect(manifest.icons?.some((icon) => icon.purpose === 'maskable')).toBe(true)
  })
})
