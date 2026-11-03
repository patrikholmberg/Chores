# Restart keeps the Chore Attempt's original start day, bounded by expiry

A Chore Attempt counts toward the Family Day it was started on, and a Restart (e.g. laundry must be rewashed) sends the same attempt back to its first Step instead of creating a new one, so it keeps that start day. To stop an attempt from claiming an old day indefinitely, attempts left unfinished past a Family-configurable window (default 7 days) become Abandoned and earn nothing.

## Consequences

- Nightly Settlement settles a day without its unfinished attempts, then re-settles that day when an attempt completes, even if the Point Period has already closed (see ADR-0001).
- The expiry window bounds how far back re-settlement can reach.
