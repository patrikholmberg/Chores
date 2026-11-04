namespace Chores.Domain.Common;

/// <summary>Thrown when an operation would break a domain rule. <see cref="Code"/> is stable and safe to show to clients.</summary>
public sealed class DomainRuleViolation(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
