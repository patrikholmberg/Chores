namespace Chores.Domain.ChoreAttempts;

/// <summary>Stable codes of <see cref="Common.DomainRuleViolation"/>s raised by the Chore Attempt aggregate.</summary>
public static class AttemptErrorCodes
{
    public const string ChoreArchived = "attempt.chore_archived";
    public const string NotOpen = "attempt.not_open";
    public const string NotCurrentStep = "attempt.not_current_step";
    public const string NotComplete = "attempt.not_complete";
    public const string RejectionWindowClosed = "attempt.rejection_window_closed";
    public const string RejectionReasonTooLong = "attempt.rejection_reason_too_long";
}
