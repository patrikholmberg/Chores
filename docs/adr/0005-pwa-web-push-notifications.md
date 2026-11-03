# Notifications via PWA and Web Push, not a native app

The React client is an installable PWA and the backend delivers notifications with standard Web Push (VAPID). This keeps the client a plain container with no app stores, signing or Firebase/APNs accounts, which suits a self-hosted family app.

## Consequences

- On iOS, push only works for 16.4+ and only after the app is added to the home screen; onboarding must guide this.
- Web Push requires HTTPS, so the self-hosted deployment needs a TLS reverse proxy.
- If delivery proves unreliable, a Capacitor wrapper with FCM/APNs is the fallback.
