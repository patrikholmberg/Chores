import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap/js/dist/collapse'
import './index.css'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { I18nextProvider } from 'react-i18next'
import App from './App.tsx'
import { createUserManager } from './auth/userManager'
import { createI18n } from './i18n'
import { followSystemColorScheme } from './shell/colorScheme'

followSystemColorScheme()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <I18nextProvider i18n={createI18n()}>
      <App userManager={createUserManager()} />
    </I18nextProvider>
  </StrictMode>,
)
