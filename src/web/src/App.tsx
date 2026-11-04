import type { UserManager } from 'oidc-client-ts'
import { BrowserRouter, Route, Routes } from 'react-router'
import { AuthCallback } from './auth/AuthCallback'
import { AuthProvider } from './auth/AuthProvider'
import { RequireAuth, RequireParent } from './auth/RequireAuth'
import { callbackPath } from './auth/userManager'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { MemberFormPage } from './pages/MemberFormPage'
import { MembersPage } from './pages/MembersPage'
import { SignupPage } from './pages/SignupPage'
import { Layout } from './shell/Layout'

export function AppRoutes({ userManager }: { userManager: UserManager }) {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/signup" element={<SignupPage />} />
        <Route path={callbackPath} element={<AuthCallback userManager={userManager} />} />
        <Route element={<RequireAuth />}>
          <Route index element={<HomePage />} />
          <Route element={<RequireParent />}>
            <Route path="/members" element={<MembersPage />} />
            <Route path="/members/new" element={<MemberFormPage />} />
            <Route path="/members/:id" element={<MemberFormPage />} />
          </Route>
        </Route>
        <Route path="*" element={<HomePage />} />
      </Route>
    </Routes>
  )
}

export default function App({ userManager }: { userManager: UserManager }) {
  return (
    <AuthProvider userManager={userManager}>
      <BrowserRouter>
        <AppRoutes userManager={userManager} />
      </BrowserRouter>
    </AuthProvider>
  )
}
