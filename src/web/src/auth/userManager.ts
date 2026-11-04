import { UserManager, WebStorageStateStore } from 'oidc-client-ts'

export const clientId = 'chores-web'
export const callbackPath = '/auth/callback'

/**
 * Authorization Code + PKCE against the Api's OpenIddict server on the same origin. Tokens live in localStorage so an
 * installed app stays signed in across restarts; renewal uses the refresh token, not a hidden iframe.
 */
export function createUserManager(origin: string = window.location.origin): UserManager {
  return new UserManager({
    authority: origin,
    client_id: clientId,
    redirect_uri: origin + callbackPath,
    post_logout_redirect_uri: origin + '/',
    response_type: 'code',
    scope: 'openid profile offline_access',
    userStore: new WebStorageStateStore({ store: window.localStorage }),
    automaticSilentRenew: true,
    loadUserInfo: false,
    monitorSession: false,
  })
}
