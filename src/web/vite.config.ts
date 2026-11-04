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
          '/api': { target: apiTarget, changeOrigin: true, secure: false },
          '/connect': { target: apiTarget, changeOrigin: true, secure: false },
        }
      : undefined,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
