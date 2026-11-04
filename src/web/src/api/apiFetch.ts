import { ApiError } from './ApiError'

export interface TokenSource {
  /** The current access token, if signed in. */
  accessToken(): Promise<string | undefined>
  /** Tries to get a fresh access token with the refresh token. Returns false when the session is over. */
  renew(): Promise<boolean>
}

export type ApiFetch = <T = void>(path: string, init?: { method?: string; body?: unknown }) => Promise<T>

/**
 * Calls the Api with the bearer token. On a 401 it renews once and retries, so an expired access token is invisible
 * to callers. Problems are thrown as {@link ApiError}.
 */
export function createApiFetch(tokens: TokenSource | undefined, fetchImpl: typeof fetch = (...args) => fetch(...args)): ApiFetch {
  return async function apiFetch<T>(path: string, init: { method?: string; body?: unknown } = {}): Promise<T> {
    const send = async () => {
      const headers: Record<string, string> = { Accept: 'application/json' }
      const token = await tokens?.accessToken()
      if (token) {
        headers.Authorization = `Bearer ${token}`
      }
      if (init.body !== undefined) {
        headers['Content-Type'] = 'application/json'
      }
      return fetchImpl(path, {
        method: init.method ?? 'GET',
        headers,
        body: init.body === undefined ? undefined : JSON.stringify(init.body),
      })
    }

    let response = await send()
    if (response.status === 401 && tokens && (await tokens.renew())) {
      response = await send()
    }

    if (!response.ok) {
      throw await ApiError.fromResponse(response)
    }

    const text = await response.text()
    return (text ? JSON.parse(text) : undefined) as T
  }
}
