/** A full-page navigation, e.g. back to the authorization server. Separate so tests can replace it. */
export function redirectTo(url: string) {
  window.location.assign(url)
}
