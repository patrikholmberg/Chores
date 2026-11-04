using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Features.Members;
using Mediator;

namespace Chores.Application.Features.ChoreAttempts;

/// <summary>One attempt with its Steps. A Child only sees their own attempts; to them, a sibling's does not exist.</summary>
public sealed record GetAttemptQuery(Guid AttemptId) : IQuery<AttemptDto>;

public sealed class GetAttemptHandler(
    IChoreAttemptRepository attempts,
    IFamilyRepository families,
    ICurrentMember currentMember,
    TimeProvider time) : IQueryHandler<GetAttemptQuery, AttemptDto>
{
    public async ValueTask<AttemptDto> Handle(GetAttemptQuery query, CancellationToken cancellationToken)
    {
        var attempt = await attempts.LoadVisibleAsync(query.AttemptId, currentMember, cancellationToken);
        var family = await families.GetRequiredAsync(cancellationToken);
        return AttemptDto.From(attempt, new MemberNames(family), time.GetUtcNow());
    }
}
