# Points are an append-only ledger with the floor at 0 applied to totals

Every points movement (Day Result, re-settlement correction, period deposit, Redemption hold/release/deduction, Adjustment) is an immutable Points Ledger entry; Period Score and Wallet are derived sums, cached as projections. Re-settlement appends corrections instead of editing history, so anyone can see why a number changed. The "never below 0" rule is applied to the total (`max(0, Baseline + ΣDay Results)`), not day by day, so results don't depend on the order corrections arrive.

## Considered Options

- Mutable totals with an audit table: simpler reads, but corrections and clamping are easy to get subtly wrong.
- Per-day clamping: more forgiving to the Child, but re-settling an early day changes the clamping of every later day.
