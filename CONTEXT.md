# Family Chores

A gamified chore tracker for a household: children earn or lose points depending on how many chores they complete each day, as configured by their parents.

## Language

### Family

**Family**:
The tenant: a household and everything it owns (members, chores, settings).
_Avoid_: Tenant, household, account

**Family Day**:
A calendar day measured in the Family's time zone. Every day boundary, reminder time and Settlement uses it.
_Avoid_: Day (unqualified), date

**Member**:
A person in a Family with their own login, who is either a Parent or a Child.
_Avoid_: User, account

**Parent**:
A Member who configures the Family, its Chore Library and each Child's rules.

**Child**:
A Member who does chores and earns or loses points.
_Avoid_: Kid

### Points

**Base Points**:
The Family-wide unit value of one standard chore. Every other points quantity is a multiple of it.
_Avoid_: Chore points, unit

**Daily Minimum**:
The number of Base Points a Child must earn in a day to avoid losing points (configured as a chore count × Base Points).
_Avoid_: Quota, required chores

**Daily Maximum**:
The most points a Child's completed chores can count for in one day. Points earned above it are discarded, not carried over.
_Avoid_: Cap, limit

**Day Result**:
A Child's points earned for a day (capped at the Daily Maximum) minus the Daily Minimum. It can be negative (a deduction) or positive (a bonus).
_Avoid_: Daily score, delta

**Settlement**:
The nightly calculation of a Child's Day Result. A day is settled the first time without unfinished chores, and settled again when a chore started that day completes later, even if its Point Period has already closed.
_Avoid_: Calculation, scoring run

**Point Period**:
The per-Child span (Weekly or Monthly) over which Day Results add up into a Period Score.
_Avoid_: Week, cycle

**Baseline**:
The Period Score a Child starts each Point Period with.
_Avoid_: Starting points, allowance

**Period Score**:
A Child's running score within one Point Period: the Baseline plus that period's Day Results, never below 0. It is what the progress indicator shows.
_Avoid_: Current points, balance

**Excused Day**:
A Family Day on which a Parent has released a Child from the Daily Minimum (sick, holiday, away). Chores done that day still earn up to the Daily Maximum.
_Avoid_: Day off, exemption

**Rejection**:
A Parent overturning a completed Chore Attempt after the fact, so that it no longer earns points and its day is re-settled.
_Avoid_: Disapproval, undo

**Points Ledger**:
The permanent, append-only history of every points movement for a Child. Period Score and Wallet are always derived from it.
_Avoid_: Transactions, history log

**Adjustment**:
A manual Wallet credit or debit by a Parent, with a stated reason, outside of chores.
_Avoid_: Bonus, penalty, correction

**Wallet**:
A Child's spendable points balance, carried across Point Periods and never below 0. It grows when a closed Point Period's Period Score is deposited (plus any later corrections) and shrinks when Rewards are redeemed.
_Avoid_: Balance, account, bank

**Reward**:
Something a Parent offers in the Family's reward shop at a points price, such as allowance or screen time.
_Avoid_: Prize, item

### Chores

**Chore Library**:
The set of Chores defined by a Family's Parents. Any Child may pick any Chore from it at any time; chores are never assigned.
_Avoid_: Chore list, catalog

**Chore**:
A Library entry with a title, description, Point Multiplier and one or more ordered Steps. Completing all its Steps earns Point Multiplier × Base Points.
_Avoid_: Task, job

**Point Multiplier**:
A whole number, at least 1, giving how many Base Points a Chore is worth.
_Avoid_: Weight, value

**Step**:
One ordered part of a Chore. A Step may carry a reminder delay measured from when the previous Step was completed.
_Avoid_: Subtask, stage

**Chore Attempt**:
One Child's run through a Chore's Steps. It counts toward the Family day on which it was started, wherever it finishes.
_Avoid_: Instance, execution, session

**Restart**:
Sending a Chore Attempt back to its first Step. It stays the same attempt and keeps its original start day.
_Avoid_: Reset, redo

**Abandoned**:
The state of a Chore Attempt that expired unfinished after the Family's attempt window. It never earns points.
_Avoid_: Cancelled, failed

**Redemption**:
A Child's request to exchange Wallet points for a Reward. Its price is held when requested, then deducted if a Parent approves or released if the Parent rejects.
_Avoid_: Purchase, order

### Notifications

**Daily Reminder**:
A notification telling a Child how many points they've earned today against their Daily Minimum, or congratulating them if they've already met it.
_Avoid_: Nudge, alert

**Reminder Schedule**:
The weekly pattern of Daily Reminder times (zero or more per weekday). The Family has a default; a Child's own schedule replaces it entirely.
_Avoid_: Notification settings, alarm

**Step Reminder**:
A notification asking a Child to do the next Step of an open Chore Attempt. It is due a configured delay after the previous Step and repeats at an interval until done.
_Avoid_: Follow-up

**Quiet Hours**:
A nightly window, set for the Family and overridable per Child, during which Step Reminders are held back until it ends.
_Avoid_: Do not disturb, night mode
