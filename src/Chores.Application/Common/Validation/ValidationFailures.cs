using FluentValidation;
using FluentValidation.Results;

namespace Chores.Application.Common.Validation;

public static class ValidationFailures
{
    /// <summary>A validation failure found by a handler rather than a validator, such as one that needs the database.</summary>
    public static ValidationException For(string property, string code, string message) =>
        new([new ValidationFailure(property, message) { ErrorCode = code }]);

    public static ValidationException UsernameTaken() =>
        For("Username", ValidationCodes.UsernameTaken, "That username is already taken.");
}
