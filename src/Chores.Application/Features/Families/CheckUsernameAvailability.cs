using Chores.Application.Common.Identity;
using Chores.Application.Common.Validation;
using Chores.Domain.Families;
using FluentValidation;
using Mediator;

namespace Chores.Application.Features.Families;

/// <summary>Whether a username is free across all Families. Used by signup and when adding Members.</summary>
public sealed record CheckUsernameAvailabilityQuery(string Username) : IQuery<UsernameAvailability>;

public sealed record UsernameAvailability(bool Available);

public sealed class CheckUsernameAvailabilityValidator : AbstractValidator<CheckUsernameAvailabilityQuery>
{
    public CheckUsernameAvailabilityValidator() => RuleFor(q => q.Username).ValidUsername();
}

public sealed class CheckUsernameAvailabilityHandler(IMemberCredentials credentials)
    : IQueryHandler<CheckUsernameAvailabilityQuery, UsernameAvailability>
{
    public async ValueTask<UsernameAvailability> Handle(CheckUsernameAvailabilityQuery query, CancellationToken cancellationToken) =>
        new(!await credentials.IsUsernameTakenAsync(Username.Create(query.Username), null, cancellationToken));
}
