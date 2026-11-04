namespace Chores.Application.Common.Exceptions;

/// <summary>A save hit the global unique username index, typically from a race between two requests.</summary>
public sealed class UsernameTakenException(Exception? innerException = null)
    : Exception("The username is already taken.", innerException);
