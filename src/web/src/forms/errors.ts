import type { TFunction } from 'i18next'
import { ApiError } from '../api/ApiError'

/** Minimum password lengths, mirroring the Api's PasswordPolicy. */
export const minimumPasswordLength = { parent: 10, child: 4 } as const

/** The translated message for an Api error code, e.g. `username.taken`. */
export function errorMessage(t: TFunction, code: string | undefined, values: Record<string, unknown> = {}): string {
  // Codes come from the Api, so the key is only known at runtime; unknown codes fall back to the generic message.
  const key = `errors.${code ?? 'generic'}` as 'errors.generic'
  return t(key, { defaultValue: t('errors.generic'), ...values })
}

/** Translated messages for one form field and for the form as a whole, from whatever the last submit threw. */
export function describeError(t: TFunction, error: unknown, values: Record<string, unknown> = {}) {
  const apiError = error instanceof ApiError ? error : undefined
  return {
    field: (name: string) => {
      const code = apiError?.fieldCode(name)
      return code ? errorMessage(t, code, values) : undefined
    },
    form:
      error === undefined
        ? undefined
        : apiError && Object.keys(apiError.fieldCodes).length > 0
          ? undefined
          : apiError?.status === 429
            ? t('errors.rate_limited')
            : errorMessage(t, apiError?.code, values),
  }
}
