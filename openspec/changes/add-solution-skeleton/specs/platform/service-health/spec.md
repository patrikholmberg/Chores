# Spec Delta

## Purpose

Lets operators and the container runtime tell whether the Api and Worker services are running and able to reach their database.

## ADDED Requirements

### Requirement: Liveness endpoint
The Api and Worker services SHALL each expose an unauthenticated liveness endpoint at `/alive` that reports healthy whenever the process is running, regardless of the state of its dependencies.

#### Scenario: Process running with database down
- **WHEN** the database is unreachable and `/alive` is requested
- **THEN** the service responds with HTTP 200

### Requirement: Health endpoint reports database reachability
The Api and Worker services SHALL each expose an unauthenticated health endpoint at `/health` that reports unhealthy when the PostgreSQL database cannot be reached.

#### Scenario: Database reachable
- **WHEN** the database is reachable and `/health` is requested
- **THEN** the service responds with HTTP 200

#### Scenario: Database unreachable
- **WHEN** the database cannot be reached and `/health` is requested
- **THEN** the service responds with HTTP 503

### Requirement: Health endpoints disclose no internal details
Health and liveness responses SHALL NOT include connection strings, hostnames, exception messages or stack traces.

#### Scenario: Unhealthy response body
- **WHEN** `/health` reports unhealthy
- **THEN** the response body contains only an overall status, with no connection or exception details
