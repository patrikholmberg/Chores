/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'
import { pwaOptions } from './pwa.config.ts'

const apiTarget = process.env.API_HTTPS || process.env.API_HTTP

export default defineConfig({
  plugins: [react(), VitePWA(pwaOptions)],
  server: {
    port: Number(process.env.PORT) || 5173,
    proxy: apiTarget
      ? {
          // Keep the browser's Host and forward the scheme, as the production reverse proxy does: the authorization
          // server builds its endpoint URLs from them, and they must point at this origin, not the Api's own port.
          '/api': { target: apiTarget, changeOrigin: false, xfwd: true, secure: false },
          '/connect': { target: apiTarget, changeOrigin: false, xfwd: true, secure: false },
          // OpenID Connect discovery and signing keys, read by oidc-client-ts before sign-in.
          '/.well-known': { target: apiTarget, changeOrigin: false, xfwd: true, secure: false },
        }
      : undefined,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
