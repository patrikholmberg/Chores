namespace Chores.Application.Common.Exceptions;

/// <summary>Someone else changed the same data first. Mapped to HTTP 409.</summary>
public sealed class ConcurrencyConflictException(Exception? innerException = null)
    : Exception("The data was changed by someone else. Reload and try again.", innerException);
