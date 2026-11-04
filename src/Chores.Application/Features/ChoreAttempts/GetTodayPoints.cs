using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Features.Members;
using Chores.Domain.Families;
using Mediator;

namespace Chores.Application.Features.ChoreAttempts;

/// <summary>Today's Family Day with each Child's points earned so far.</summary>
public sealed record TodayPointsDto(DateOnly Day, int BasePoints, IReadOnlyList<ChildPointsDto> Children);

public sealed record ChildPointsDto(Guid ChildId, string ChildName, int Points);

/// <summary>
/// The points earned today: Point Multiplier × the current Base Points over the attempts started today that are complete
/// and not rejected. A Child sees only their own total, a Parent every current Child's, 0 included.
/// </summary>
public sealed record GetTodayPointsQuery : IQuery<TodayPointsDto>;

public sealed class GetTodayPointsHandler(
    IChoreAttemptRepository attempts,
    IFamilyRepository families,
    ICurrentMember currentMember,
    TimeProvider time) : IQueryHandler<GetTodayPointsQuery, TodayPointsDto>
{
    public async ValueTask<TodayPointsDto> Handle(GetTodayPointsQuery query, CancellationToken cancellationToken)
    {
        var family = await families.GetRequiredAsync(cancellationToken);
        var now = time.GetUtcNow();
        var today = family.TimeZone.DayOf(now);

        var pointsByChild = (await attempts.ListStartedOnAsync(today, cancellationToken))
            .GroupBy(a => a.ChildId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.EarnedPoints(family.BasePoints, now)));

        var children = family.Members
            .Where(m => m.Role == Role.Child)
            .Where(m => currentMember.Role == Role.Parent || m.Id == currentMember.MemberId)
            .OrderBy(m => m.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(m => new ChildPointsDto(m.Id, m.DisplayName, pointsByChild.GetValueOrDefault(m.Id)))
            .ToList();

        return new TodayPointsDto(today, family.BasePoints, children);
    }
}
