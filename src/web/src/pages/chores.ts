import type { TFunction } from 'i18next'

/** A Step as returned by `/api/chores/:id`, with reminder settings in whole minutes. */
export interface Step {
  title: string
  description: string | null
  reminderDelayMinutes: number | null
  repeatIntervalMinutes: number | null
}

/** A Chore with its Steps; `revision` must be sent back when editing it. */
export interface Chore {
  id: string
  title: string
  description: string | null
  pointMultiplier: number
  revision: number
  archived: boolean
  steps: Step[]
}

/** A Chore as listed by `/api/chores` and `/api/chores/archived`. */
export interface ChoreSummary {
  id: string
  title: string
  description: string | null
  pointMultiplier: number
  stepCount: number
}

/** Whole minutes as short text, e.g. 90 → "1 h 30 min" and 2880 → "2 d". */
export function formatDuration(t: TFunction, totalMinutes: number): string {
  const days = Math.floor(totalMinutes / 1440)
  const hours = Math.floor((totalMinutes % 1440) / 60)
  const minutes = totalMinutes % 60
  return [
    days > 0 && t('duration.days', { count: days }),
    hours > 0 && t('duration.hours', { count: hours }),
    minutes > 0 && t('duration.minutes', { count: minutes }),
  ]
    .filter(Boolean)
    .join(' ')
}
