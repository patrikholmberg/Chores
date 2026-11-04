namespace Chores.Application.Common.Exceptions;

/// <summary>The caller is signed in but may not perform the operation. Mapped to HTTP 403.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
