using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Features.Members;
using Mediator;

namespace Chores.Application.Features.FamilySettings;

/// <summary>The Family's settings, as a Parent edits them.</summary>
public sealed record GetFamilySettingsQuery : IQuery<FamilySettingsDto>, IParentOnly;

public sealed class GetFamilySettingsHandler(IFamilyRepository families) : IQueryHandler<GetFamilySettingsQuery, FamilySettingsDto>
{
    public async ValueTask<FamilySettingsDto> Handle(GetFamilySettingsQuery query, CancellationToken cancellationToken) =>
        FamilySettingsDto.From(await families.GetRequiredAsync(cancellationToken));
}
