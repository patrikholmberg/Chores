/**
 * Bootstrap 5.3 only switches to dark mode through `data-bs-theme`, so mirror the system setting onto <html> and keep
 * it in sync. Returns a function that stops following.
 */
export function followSystemColorScheme(root: HTMLElement = document.documentElement): () => void {
  if (typeof window.matchMedia !== 'function') {
    root.setAttribute('data-bs-theme', 'light')
    return () => {}
  }

  const dark = window.matchMedia('(prefers-color-scheme: dark)')
  const apply = () => root.setAttribute('data-bs-theme', dark.matches ? 'dark' : 'light')
  apply()
  dark.addEventListener('change', apply)
  return () => dark.removeEventListener('change', apply)
}
