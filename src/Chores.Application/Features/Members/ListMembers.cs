using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.Families;
using Mediator;

namespace Chores.Application.Features.Members;

public sealed record ListMembersQuery : IQuery<IReadOnlyList<MemberDto>>, IParentOnly;

public sealed class ListMembersHandler(IFamilyRepository families) : IQueryHandler<ListMembersQuery, IReadOnlyList<MemberDto>>
{
    public async ValueTask<IReadOnlyList<MemberDto>> Handle(ListMembersQuery query, CancellationToken cancellationToken)
    {
        var family = await families.GetRequiredAsync(cancellationToken);
        return family.Members
            .OrderBy(m => m.Role == Role.Parent ? 0 : 1)
            .ThenBy(m => m.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(MemberDto.From)
            .ToList();
    }
}
