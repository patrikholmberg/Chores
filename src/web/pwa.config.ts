import type { VitePWAOptions } from 'vite-plugin-pwa'

export const manifest: VitePWAOptions['manifest'] = {
  name: 'Chores',
  short_name: 'Chores',
  description: 'Family chores, points and rewards',
  lang: 'en',
  start_url: '/',
  scope: '/',
  display: 'standalone',
  theme_color: '#2f7d5b',
  background_color: '#f7f7f5',
  icons: [
    { src: 'pwa-192x192.png', sizes: '192x192', type: 'image/png' },
    { src: 'pwa-512x512.png', sizes: '512x512', type: 'image/png' },
    { src: 'maskable-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
  ],
}

export const pwaOptions: Partial<VitePWAOptions> = {
  registerType: 'autoUpdate',
  injectRegister: 'auto',
  includeAssets: ['favicon.svg', 'apple-touch-icon-180x180.png'],
  manifest,
  workbox: {
    globPatterns: ['**/*.{js,css,html,svg,png,woff2}'],
    navigateFallback: '/index.html',
    // Server routes must never be answered by the cached app shell.
    navigateFallbackDenylist: [/^\/api\//, /^\/connect\//, /^\/health/, /^\/alive/],
  },
}
