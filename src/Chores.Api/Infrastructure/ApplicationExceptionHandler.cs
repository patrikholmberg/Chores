using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Validation;
using Chores.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Chores.Api.Infrastructure;

/// <summary>Maps Application and Domain exceptions to RFC 7807 responses with a stable <c>code</c> where clients need one.</summary>
internal sealed class ApplicationExceptionHandler(IProblemDetailsService problemDetails, ValidationExceptionHandler validation)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is UsernameTakenException)
        {
            return await validation.TryHandleAsync(httpContext, ValidationFailures.UsernameTaken(), cancellationToken);
        }

        (int Status, string Title, string? Code)? problem = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found.", null),
            ForbiddenException => (StatusCodes.Status403Forbidden, exception.Message, "forbidden"),
            DomainRuleViolation rule => (StatusCodes.Status409Conflict, rule.Message, rule.Code),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, exception.Message, "concurrency_conflict"),
            // A body that does not bind, such as a fractional number for a whole-number field.
            BadHttpRequestException badRequest => (badRequest.StatusCode, "The request could not be read.", null),
            _ => null,
        };
        if (problem is not { } p)
        {
            return false;
        }

        httpContext.Response.StatusCode = p.Status;
        var details = new ProblemDetails { Status = p.Status, Title = p.Title };
        if (p.Code is not null)
        {
            details.Extensions["code"] = p.Code;
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = details,
        });
    }
}
