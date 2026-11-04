import { useEffect } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router'
import { Loading } from '../shell/Loading'
import { useAuth } from './AuthContext'

/** Renders its routes for a signed-in Member; otherwise starts sign-in and comes back to the same path. */
export function RequireAuth() {
  const { status, signIn } = useAuth()
  const location = useLocation()
  const target = location.pathname + location.search

  useEffect(() => {
    if (status === 'signedOut') {
      void signIn(target)
    }
  }, [status, signIn, target])

  return status === 'signedIn' ? <Outlet /> : <Loading />
}

/** Renders its routes only for Parents; Children are sent to the home screen. */
export function RequireParent() {
  const { member } = useAuth()
  return member?.role === 'parent' ? <Outlet /> : <Navigate to="/" replace />
}
