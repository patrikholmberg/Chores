using System.Diagnostics.CodeAnalysis;

namespace Chores.Domain.Families;

/// <summary>The IANA time zone that defines a Family's Family Day.</summary>
public sealed record FamilyTimeZone
{
    private FamilyTimeZone(string id) => Id = id;

    public string Id { get; }

    /// <summary>Accepts only IANA ids (e.g. <c>Europe/Stockholm</c>); Windows ids and unknown zones are rejected.</summary>
    public static bool TryCreate(string? id, [NotNullWhen(true)] out FamilyTimeZone? timeZone)
    {
        timeZone = !string.IsNullOrWhiteSpace(id)
            && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)
            && zone.HasIanaId
                ? new FamilyTimeZone(zone.Id)
                : null;
        return timeZone is not null;
    }

    public static FamilyTimeZone Create(string id) =>
        TryCreate(id, out var timeZone)
            ? timeZone
            : throw new ArgumentException($"'{id}' is not an IANA time zone.", nameof(id));

    /// <summary>The Family Day that <paramref name="instant"/> falls on.</summary>
    public DateOnly DayOf(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.FindSystemTimeZoneById(Id)).DateTime);

    public override string ToString() => Id;
}
