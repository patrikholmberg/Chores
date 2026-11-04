using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Mediator;

namespace Chores.Application.Features.ChoreLibrary;

/// <summary>The Chore Library: every Chore that is not archived. Any Member of the Family may read it.</summary>
public sealed record ListChoresQuery : IQuery<IReadOnlyList<ChoreSummaryDto>>;

public sealed class ListChoresHandler(IChoreRepository chores, IFamilyRepository families, ICurrentMember currentMember)
    : IQueryHandler<ListChoresQuery, IReadOnlyList<ChoreSummaryDto>>
{
    public async ValueTask<IReadOnlyList<ChoreSummaryDto>> Handle(ListChoresQuery query, CancellationToken cancellationToken) =>
        await (await chores.ListAsync(archived: false, cancellationToken))
            .SortedByTitleAsync(families, currentMember, cancellationToken);
}
