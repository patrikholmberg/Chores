using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.Families;
using Mediator;

namespace Chores.Application.Features.ChoreLibrary;

/// <summary>One Chore with its Steps. Only Parents see archived Chores; to a Child they do not exist.</summary>
public sealed record GetChoreQuery(Guid ChoreId) : IQuery<ChoreDto>;

public sealed class GetChoreHandler(IChoreRepository chores, ICurrentMember currentMember) : IQueryHandler<GetChoreQuery, ChoreDto>
{
    public async ValueTask<ChoreDto> Handle(GetChoreQuery query, CancellationToken cancellationToken)
    {
        var chore = await chores.GetRequiredAsync(query.ChoreId, cancellationToken);
        return chore.IsArchived && currentMember.Role != Role.Parent
            ? throw new NotFoundException("Chore", query.ChoreId)
            : ChoreDto.From(chore);
    }
}
