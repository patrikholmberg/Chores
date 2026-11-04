using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Mediator;

namespace Chores.Application.Features.Members;

public sealed record GetMemberQuery(Guid MemberId) : IQuery<MemberDto>, IParentOnly;

public sealed class GetMemberHandler(IFamilyRepository families) : IQueryHandler<GetMemberQuery, MemberDto>
{
    public async ValueTask<MemberDto> Handle(GetMemberQuery query, CancellationToken cancellationToken) =>
        MemberDto.From((await families.GetRequiredAsync(cancellationToken)).GetMember(query.MemberId));
}
