/** The device's IANA time zone, used to pre-fill the Family's time zone. */
export function deviceTimeZone(): string {
  return Intl.DateTimeFormat().resolvedOptions().timeZone || 'Europe/Stockholm'
}

/** Every IANA time zone the browser knows, always including `selected`. */
export function timeZones(selected: string): string[] {
  const zones = typeof Intl.supportedValuesOf === 'function' ? Intl.supportedValuesOf('timeZone') : []
  return zones.includes(selected) ? zones : [selected, ...zones]
}
