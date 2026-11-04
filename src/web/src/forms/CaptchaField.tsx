import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

/**
 * The self-hosted ALTCHA proof-of-work widget. It adds a hidden `altcha` input with the solution to the surrounding
 * form. The widget script is loaded on demand, so it only ships with the signup screen.
 */
export function CaptchaField({ error }: { error?: string }) {
  const { i18n } = useTranslation()
  const [loaded, setLoaded] = useState(false)

  useEffect(() => {
    let active = true
    void Promise.all([import('altcha'), import('altcha/i18n/sv'), import('altcha/i18n/en')]).then(() => {
      if (active) setLoaded(true)
    })
    return () => {
      active = false
    }
  }, [])

  return (
    <div className="mb-3">
      {loaded && <altcha-widget challenge="/api/captcha/challenge" name="altcha" language={i18n.resolvedLanguage} />}
      {error && <div className="invalid-feedback d-block">{error}</div>}
    </div>
  )
}
