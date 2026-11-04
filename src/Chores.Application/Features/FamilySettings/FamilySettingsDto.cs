using Chores.Domain.Families;

namespace Chores.Application.Features.FamilySettings;

/// <summary>The Family-wide settings, with the attempt window in whole days.</summary>
public sealed record FamilySettingsDto(int BasePoints, int AttemptWindowDays)
{
    public static FamilySettingsDto From(Family family) => new(family.BasePoints, (int)family.AttemptWindow.TotalDays);
}
