namespace Chores.Domain.ChoreAttempts;

/// <summary>Where a Chore Attempt stands at a given moment. Abandoned is derived from the expiry time, never stored.</summary>
public enum AttemptState
{
    Open,
    Complete,
    Rejected,
    Abandoned,
}
