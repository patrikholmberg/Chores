# Spec Delta

## Purpose

Provides the installable app shell that every Member uses on their phone or computer, including choosing between Swedish and English.

## ADDED Requirements

### Requirement: Installable as a PWA
The web client SHALL be installable to a phone home screen as a Progressive Web App, with an app name, icons and standalone display mode.

#### Scenario: Install on Android
- **WHEN** a user opens the client over HTTPS in Chrome on Android
- **THEN** the browser offers to install the app, and the installed app opens without browser UI

#### Scenario: Add to home screen on iOS
- **WHEN** a user adds the client to the home screen from Safari on iOS 16.4 or later
- **THEN** the app opens in standalone mode with its own icon and name

### Requirement: Shell loads offline
The web client SHALL serve its app shell from cache when the device is offline, showing an offline message instead of a browser error page.

#### Scenario: Opened without network
- **WHEN** an installed client is opened with no network connection
- **THEN** the app shell renders and indicates that the device is offline

### Requirement: Swedish and English UI
The web client SHALL display all user-visible text in either Swedish or English, and no screen SHALL show untranslated keys.

#### Scenario: First visit language
- **WHEN** a user opens the client for the first time with a browser language of Swedish
- **THEN** the UI is shown in Swedish

#### Scenario: Unsupported browser language
- **WHEN** a user opens the client for the first time with a browser language other than Swedish or English
- **THEN** the UI is shown in English

### Requirement: Language can be switched
The web client SHALL let the user switch between Swedish and English at any time, apply it immediately and remember the choice on that device.

#### Scenario: Switch and reload
- **WHEN** the user switches the language to English and then reloads the app
- **THEN** the UI is shown in English
