# Self-hosted OAuth 2.0 with OpenIddict and ASP.NET Identity

Authentication uses OpenIddict as an OAuth 2.0 / OIDC authorization server inside our .NET solution, with credentials stored via ASP.NET Identity in our own PostgreSQL. The React PWA uses Authorization Code + PKCE. We chose this over Keycloak because Parents create and manage Child logins (globally unique usernames, no email, password reset done by a Parent), which fits our Family/Member model directly instead of being mirrored through an external IdP's admin API.

## Consequences

- We own security-sensitive code (lockout, token lifetimes, key rotation) and must keep it current.
- Self-service Family signup requires an email provider for Parent password reset.
