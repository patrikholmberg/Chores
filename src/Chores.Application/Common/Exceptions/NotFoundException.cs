namespace Chores.Application.Common.Exceptions;

/// <summary>The requested resource does not exist in the caller's Family. Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string resource, object id) : Exception($"{resource} '{id}' was not found.");
