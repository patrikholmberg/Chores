import { describe, expect, it, vi } from 'vitest'
import { ApiError } from './ApiError'
import { createApiFetch, type TokenSource } from './apiFetch'

function json(status: number, body?: unknown) {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  })
}

function tokens(initial: string, renewed?: string): TokenSource & { current: string } {
  const source = {
    current: initial,
    accessToken: async () => source.current,
    renew: vi.fn(async () => {
      if (!renewed) return false
      source.current = renewed
      return true
    }),
  }
  return source
}

describe('apiFetch', () => {
  it('sends the bearer token and parses JSON', async () => {
    const fetchImpl = vi.fn(async () => json(200, { ok: true }))
    const apiFetch = createApiFetch(tokens('abc'), fetchImpl)

    await expect(apiFetch('/api/me')).resolves.toEqual({ ok: true })
    expect(fetchImpl).toHaveBeenCalledWith('/api/me', expect.objectContaining({
      headers: expect.objectContaining({ Authorization: 'Bearer abc' }),
    }))
  })

  it('renews once after a 401 and retries with the new token', async () => {
    const fetchImpl = vi.fn()
      .mockResolvedValueOnce(json(401))
      .mockResolvedValueOnce(json(200, { ok: true }))
    const source = tokens('old', 'new')

    await expect(createApiFetch(source, fetchImpl)('/api/me')).resolves.toEqual({ ok: true })
    expect(source.renew).toHaveBeenCalledOnce()
    expect(fetchImpl.mock.calls[1][1].headers.Authorization).toBe('Bearer new')
  })

  it('gives up when the session cannot be renewed', async () => {
    const fetchImpl = vi.fn(async () => json(401))

    await expect(createApiFetch(tokens('old'), fetchImpl)('/api/me')).rejects.toMatchObject({ status: 401 })
    expect(fetchImpl).toHaveBeenCalledOnce()
  })

  it('turns validation problems into field codes', async () => {
    const fetchImpl = vi.fn(async () => json(400, {
      title: 'One or more validation errors occurred.',
      errorCodes: { Username: ['username.taken'] },
    }))

    const error = await createApiFetch(undefined, fetchImpl)('/api/families', { method: 'POST', body: {} }).catch((e) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error.fieldCode('username')).toBe('username.taken')
  })

  it('returns undefined for an empty response', async () => {
    const fetchImpl = vi.fn(async () => new Response(null, { status: 204 }))

    await expect(createApiFetch(tokens('t'), fetchImpl)('/api/me/language', { method: 'PUT', body: {} })).resolves.toBeUndefined()
  })
})
