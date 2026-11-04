using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Features.Members;
using Chores.Domain.Families;
using Mediator;

namespace Chores.Application.Features.ChoreAttempts;

/// <summary>
/// Recent attempts, newest first: those started on any of the last attempt-window Family Days, today included. A Child
/// sees their own, a Parent those of every Child.
/// </summary>
public sealed record ListAttemptsQuery : IQuery<IReadOnlyList<AttemptSummaryDto>>;

public sealed class ListAttemptsHandler(
    IChoreAttemptRepository attempts,
    IFamilyRepository families,
    ICurrentMember currentMember,
    TimeProvider time) : IQueryHandler<ListAttemptsQuery, IReadOnlyList<AttemptSummaryDto>>
{
    public async ValueTask<IReadOnlyList<AttemptSummaryDto>> Handle(ListAttemptsQuery query, CancellationToken cancellationToken)
    {
        var family = await families.GetRequiredAsync(cancellationToken);
        var now = time.GetUtcNow();
        var fromDay = family.TimeZone.DayOf(now).AddDays(1 - (int)family.AttemptWindow.TotalDays);
        Guid? childId = currentMember.Role == Role.Parent ? null : currentMember.MemberId ?? Guid.Empty;

        var names = new MemberNames(family);
        return (await attempts.ListAsync(childId, fromDay, cancellationToken))
            .Select(a => AttemptSummaryDto.From(a, names, now))
            .ToList();
    }
}
