using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Mediator;

namespace Chores.Application.Features.ChoreLibrary;

/// <summary>The Family's archived Chores, which a Parent can restore.</summary>
public sealed record ListArchivedChoresQuery : IQuery<IReadOnlyList<ChoreSummaryDto>>, IParentOnly;

public sealed class ListArchivedChoresHandler(IChoreRepository chores, IFamilyRepository families, ICurrentMember currentMember)
    : IQueryHandler<ListArchivedChoresQuery, IReadOnlyList<ChoreSummaryDto>>
{
    public async ValueTask<IReadOnlyList<ChoreSummaryDto>> Handle(ListArchivedChoresQuery query, CancellationToken cancellationToken) =>
        await (await chores.ListAsync(archived: true, cancellationToken))
            .SortedByTitleAsync(families, currentMember, cancellationToken);
}
