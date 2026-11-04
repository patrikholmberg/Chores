import { describe, expect, it } from 'vitest'
import en from './en/common.json'
import sv from './sv/common.json'

function keys(value: unknown, prefix = ''): string[] {
  if (typeof value !== 'object' || value === null) {
    return [prefix]
  }
  return Object.entries(value).flatMap(([key, child]) => keys(child, prefix ? `${prefix}.${key}` : key))
}

describe('locales', () => {
  it('Swedish and English define exactly the same keys', () => {
    expect(keys(sv).sort()).toEqual(keys(en).sort())
  })

  it('has no empty translations', () => {
    for (const locale of [en, sv]) {
      for (const key of keys(locale)) {
        const text = key.split('.').reduce<unknown>((node, part) => (node as Record<string, unknown>)[part], locale)
        expect(text, key).not.toBe('')
      }
    }
  })
})
