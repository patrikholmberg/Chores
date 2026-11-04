/** Where a Chore Attempt stands. Abandoned means it was not complete before its expiry time. */
export type AttemptState = 'open' | 'complete' | 'rejected' | 'abandoned'

export interface Rejection {
  at: string
  byMemberId: string
  byName: string
  reason: string | null
}

/** A snapshotted Step of an attempt, with when it was completed. */
export interface AttemptStep {
  position: number
  title: string
  description: string | null
  reminderDelayMinutes: number | null
  repeatIntervalMinutes: number | null
  completedAt: string | null
}

/** An attempt as returned by `/api/attempts/:id`. `startDay` is a Family Day (`YYYY-MM-DD`), not an instant. */
export interface Attempt {
  id: string
  childId: string
  childName: string
  choreId: string
  title: string
  description: string | null
  pointMultiplier: number
  startDay: string
  startedAt: string
  expiresAt: string
  completedAt: string | null
  state: AttemptState
  currentStepPosition: number | null
  rejection: Rejection | null
  revision: number
  steps: AttemptStep[]
}

/** An attempt as listed by `/api/attempts`. */
export interface AttemptSummary {
  id: string
  childId: string
  childName: string
  choreId: string
  title: string
  pointMultiplier: number
  startDay: string
  startedAt: string
  expiresAt: string
  completedAt: string | null
  state: AttemptState
  stepCount: number
  currentStepPosition: number | null
  currentStepTitle: string | null
  rejection: Rejection | null
}

/** `/api/points/today`: today's Family Day and each visible Child's points so far. */
export interface TodayPoints {
  day: string
  basePoints: number
  children: { childId: string; childName: string; points: number }[]
}

/** `/api/family/settings`. */
export interface FamilySettings {
  basePoints: number
  attemptWindowDays: number
}

export const stateBadge: Record<AttemptState, string> = {
  open: 'text-bg-primary',
  complete: 'text-bg-success',
  rejected: 'text-bg-danger',
  abandoned: 'text-bg-secondary',
}

/** Whether a Parent may still reject the attempt: it is complete and its attempt window has not passed. */
export function canReject(attempt: { state: AttemptState; expiresAt: string }, now = new Date()): boolean {
  return attempt.state === 'complete' && now < new Date(attempt.expiresAt)
}

const locale = (language: string) => (language === 'sv' ? 'sv-SE' : 'en-GB')

/** A Family Day such as `2026-11-02` as "Mon 2 Nov", never shifted by the browser's time zone. */
export function formatDay(day: string, language: string): string {
  const [year, month, date] = day.split('-').map(Number)
  return new Intl.DateTimeFormat(locale(language), { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' }).format(
    new Date(Date.UTC(year, month - 1, date)),
  )
}

/** An instant as day and time in the Family's time zone, e.g. "Mon 2 Nov, 18:00". */
export function formatInstant(instant: string, language: string, timeZone: string): string {
  return new Intl.DateTimeFormat(locale(language), {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
    timeZone,
  }).format(new Date(instant))
}
