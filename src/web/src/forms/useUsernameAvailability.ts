import { useEffect, useState } from 'react'
import { ApiError } from '../api/ApiError'
import { useAuth } from '../auth/AuthContext'

export type Availability = 'idle' | 'checking' | 'available' | 'taken' | 'invalid'

/** Checks a username against every Family while the user types, waiting `delay` ms after the last keystroke. */
export function useUsernameAvailability(username: string, delay = 400): Availability {
  const { api } = useAuth()
  const [result, setResult] = useState<{ username: string; availability: Availability }>()
  const trimmed = username.trim()

  useEffect(() => {
    if (trimmed.length < 3) {
      return
    }
    let active = true
    const timer = setTimeout(() => {
      api<{ available: boolean }>(`/api/usernames/${encodeURIComponent(trimmed)}/availability`).then(
        ({ available }) => active && setResult({ username: trimmed, availability: available ? 'available' : 'taken' }),
        (error: unknown) =>
          active &&
          setResult({ username: trimmed, availability: error instanceof ApiError && error.status === 400 ? 'invalid' : 'idle' }),
      )
    }, delay)
    return () => {
      active = false
      clearTimeout(timer)
    }
  }, [trimmed, delay, api])

  if (trimmed.length < 3) return 'idle'
  return result?.username === trimmed ? result.availability : 'checking'
}
