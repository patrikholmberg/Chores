# Period Score is deposited into a separate Wallet

Each Child has a per-period Period Score (starting at the Baseline, moved by Day Results) and a long-lived Wallet that Rewards are redeemed from. The Period Score is deposited into the Wallet only when the Point Period closes; Day Results never touch the Wallet directly. We chose this so a bad week can never eat points a Child has saved towards a big Reward, and so the floor-at-0 rule on the Wallet doesn't make corrections order-dependent.

## Consequences

- Corrections to a closed period (late-finishing Chore Attempts, Rejections) are booked as separate Wallet entries rather than reopening the deposit.
- Parent Adjustments go to the Wallet, not the Period Score, keeping the progress bar purely chore-driven.
