import type { UserManager } from 'oidc-client-ts'
import { BrowserRouter, Route, Routes } from 'react-router'
import { AuthCallback } from './auth/AuthCallback'
import { AuthProvider } from './auth/AuthProvider'
import { RequireAuth, RequireParent } from './auth/RequireAuth'
import { callbackPath } from './auth/userManager'
import { ArchivedChoresPage } from './pages/ArchivedChoresPage'
import { AttemptPage } from './pages/AttemptPage'
import { ChoreFormPage } from './pages/ChoreFormPage'
import { ChorePage } from './pages/ChorePage'
import { ChoresPage } from './pages/ChoresPage'
import { FamilySettingsPage } from './pages/FamilySettingsPage'
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
          <Route path="/chores" element={<ChoresPage />} />
          <Route path="/chores/:id" element={<ChorePage />} />
          <Route path="/attempts/:id" element={<AttemptPage />} />
          <Route element={<RequireParent />}>
            <Route path="/chores/new" element={<ChoreFormPage />} />
            <Route path="/chores/archived" element={<ArchivedChoresPage />} />
            <Route path="/chores/:id/edit" element={<ChoreFormPage />} />
            <Route path="/members" element={<MembersPage />} />
            <Route path="/members/new" element={<MemberFormPage />} />
            <Route path="/members/:id" element={<MemberFormPage />} />
            <Route path="/settings" element={<FamilySettingsPage />} />
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
