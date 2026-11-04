import { vi } from 'vitest'
import type { Auth } from '../auth/AuthContext'

type Handler = (body: unknown) => unknown

/** A fake `api` that answers `METHOD /path` from a table; unknown calls resolve to undefined. */
export function fakeApi(routes: Record<string, Handler | unknown>): Auth['api'] {
  return vi.fn(async (path: string, init?: { method?: string; body?: unknown }) => {
    const route = routes[`${init?.method ?? 'GET'} ${path}`]
    return typeof route === 'function' ? (route as Handler)(init?.body) : route
  }) as unknown as Auth['api']
}
