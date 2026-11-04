import { useTranslation } from 'react-i18next'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { useAuth } from '../auth/AuthContext'
import { LanguageSwitcher } from '../i18n/LanguageSwitcher'
import { useMemberLanguage } from '../i18n/useMemberLanguage'
import { OfflineBanner } from './OfflineBanner'

export function Layout() {
  const { t } = useTranslation()
  const { status, member, signOut } = useAuth()
  const navigate = useNavigate()
  const signedIn = status === 'signedIn'
  useMemberLanguage()

  return (
    <>
      <nav className="navbar navbar-expand-sm border-bottom bg-body-tertiary">
        <div className="container">
          <h1 className="navbar-brand h4 mb-0">{t('app.title')}</h1>
          {signedIn && (
            <button
              className="navbar-toggler"
              type="button"
              data-bs-toggle="collapse"
              data-bs-target="#main-nav"
              aria-controls="main-nav"
              aria-expanded="false"
              aria-label={t('nav.toggle')}
            >
              <span className="navbar-toggler-icon" />
            </button>
          )}
          <div className={signedIn ? 'collapse navbar-collapse' : 'ms-auto'} id="main-nav">
            {signedIn && (
              <ul className="navbar-nav me-auto">
                <li className="nav-item">
                  <NavLink className="nav-link" to="/" end>
                    {t('nav.home')}
                  </NavLink>
                </li>
                <li className="nav-item">
                  <NavLink className="nav-link" to="/chores">
                    {t('nav.chores')}
                  </NavLink>
                </li>
                {member?.role === 'parent' && (
                  <>
                    <li className="nav-item">
                      <NavLink className="nav-link" to="/members">
                        {t('nav.members')}
                      </NavLink>
                    </li>
                    <li className="nav-item">
                      <NavLink className="nav-link" to="/settings">
                        {t('nav.settings')}
                      </NavLink>
                    </li>
                  </>
                )}
              </ul>
            )}
            <div className="d-flex flex-wrap align-items-center gap-2 py-2 py-sm-0">
              <LanguageSwitcher />
              {signedIn && (
                <button
                  type="button"
                  className="btn btn-sm btn-outline-secondary"
                  onClick={async () => {
                    await signOut()
                    navigate('/login', { replace: true })
                  }}
                >
                  {t('auth.signOut')}
                </button>
              )}
            </div>
          </div>
        </div>
      </nav>
      <main className="container py-3">
        <OfflineBanner />
        <Outlet />
      </main>
    </>
  )
}
