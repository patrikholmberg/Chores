using System.Globalization;
using Chores.Application.Common.Exceptions;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Domain.ChoreLibrary;
using Chores.Domain.Families;

namespace Chores.Application.Features.ChoreLibrary;

/// <summary>A Chore as it appears in the library list.</summary>
public sealed record ChoreSummaryDto(Guid Id, string Title, string? Description, int PointMultiplier, int StepCount)
{
    public static ChoreSummaryDto From(Chore chore) =>
        new(chore.Id, chore.Title, chore.Description, chore.PointMultiplier, chore.Steps.Count);
}

/// <summary>A Chore with its Steps in order. <see cref="Revision"/> must be sent back when editing it.</summary>
public sealed record ChoreDto(
    Guid Id,
    string Title,
    string? Description,
    int PointMultiplier,
    int Revision,
    bool Archived,
    IReadOnlyList<StepDto> Steps)
{
    public static ChoreDto From(Chore chore) => new(
        chore.Id,
        chore.Title,
        chore.Description,
        chore.PointMultiplier,
        chore.Revision,
        chore.IsArchived,
        chore.Steps.Select(StepDto.From).ToList());
}

/// <summary>A Step with its reminder settings in whole minutes.</summary>
public sealed record StepDto(string Title, string? Description, int? ReminderDelayMinutes, int? RepeatIntervalMinutes)
{
    public static StepDto From(Step step) => new(
        step.Title,
        step.Description,
        Minutes(step.ReminderDelay),
        Minutes(step.RepeatInterval));

    private static int? Minutes(TimeSpan? value) => value is { } v ? (int)v.TotalMinutes : null;
}

internal static class ChoreLookup
{
    /// <summary>A Chore of the current Family; Chores of other Families are reported as not found.</summary>
    public static async Task<Chore> GetRequiredAsync(this IChoreRepository chores, Guid choreId, CancellationToken cancellationToken) =>
        await chores.GetAsync(choreId, cancellationToken) ?? throw new NotFoundException("Chore", choreId);

    /// <summary>Sorts titles the way the signed-in Member reads them, so Swedish å, ä and ö come after z.</summary>
    public static async Task<IReadOnlyList<ChoreSummaryDto>> SortedByTitleAsync(
        this IEnumerable<Chore> chores,
        IFamilyRepository families,
        ICurrentMember currentMember,
        CancellationToken cancellationToken)
    {
        var family = await families.GetCurrentAsync(cancellationToken);
        var language = family?.Members.SingleOrDefault(m => m.Id == currentMember.MemberId)?.PreferredLanguage ?? PreferredLanguage.English;
        var comparer = StringComparer.Create(
            CultureInfo.GetCultureInfo(language == PreferredLanguage.Swedish ? "sv-SE" : "en-GB"),
            ignoreCase: true);

        return chores.OrderBy(c => c.Title, comparer).Select(ChoreSummaryDto.From).ToList();
    }
}
