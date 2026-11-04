import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../auth/AuthContext'

/** Once a Member is signed in, their preferred language replaces the browser or device language. */
export function useMemberLanguage() {
  const { i18n } = useTranslation()
  const { member } = useAuth()
  const preferred = member?.preferredLanguage

  // Applied only when the preference itself changes, so a switch the Member just made is never undone by a re-render.
  const applied = useRef<string | undefined>(undefined)
  useEffect(() => {
    if (preferred && applied.current !== preferred) {
      applied.current = preferred
      if (i18n.resolvedLanguage !== preferred) {
        void i18n.changeLanguage(preferred)
      }
    }
  }, [preferred, i18n])
}
