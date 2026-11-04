using Chores.Domain.Common;

namespace Chores.Domain.ChoreLibrary;

/// <summary>
/// An entry in the Family's Chore Library with its ordered Steps. Completing every Step earns
/// <see cref="PointMultiplier"/> × Base Points. Deleting a Chore archives it; an archived Chore is read-only until restored.
/// </summary>
public sealed class Chore : IFamilyOwned
{
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 1000;
    public const int MaxSteps = 20;
    public const int MinPointMultiplier = 1;
    public const int MaxPointMultiplier = 100;
    public const int DefaultPointMultiplier = 1;

    private readonly List<Step> _steps = [];

    private Chore(Guid id, Guid familyId)
    {
        Id = id;
        FamilyId = familyId;
        Title = string.Empty; // Set by Apply.
    }

    public Guid Id { get; private set; }

    public Guid FamilyId { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public int PointMultiplier { get; private set; }

    /// <summary>The Steps in the order they are done.</summary>
    public IReadOnlyList<Step> Steps => _steps.OrderBy(s => s.Position).ToList();

    /// <summary>Incremented by every change, so concurrent edits by two Parents conflict.</summary>
    public int Revision { get; private set; }

    /// <summary>When the Chore was archived (deleted from the library), or null while it is in the library.</summary>
    public DateTimeOffset? ArchivedAt { get; private set; }

    public bool IsArchived => ArchivedAt is not null;

    public static Chore Create(
        Guid id,
        Guid familyId,
        string title,
        string? description,
        int pointMultiplier,
        IReadOnlyList<StepDefinition> steps)
    {
        var chore = new Chore(id, familyId);
        chore.Apply(title, description, pointMultiplier, steps);
        return chore;
    }

    /// <summary>Replaces the Chore's fields and all its Steps.</summary>
    public void Revise(string title, string? description, int pointMultiplier, IReadOnlyList<StepDefinition> steps)
    {
        if (IsArchived)
        {
            throw new DomainRuleViolation(ChoreErrorCodes.Archived, "An archived Chore must be restored before it can be edited.");
        }

        Apply(title, description, pointMultiplier, steps);
        Revision++;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (IsArchived)
        {
            return;
        }

        ArchivedAt = archivedAt;
        Revision++;
    }

    public void Restore()
    {
        if (!IsArchived)
        {
            return;
        }

        ArchivedAt = null;
        Revision++;
    }

    private void Apply(string title, string? description, int pointMultiplier, IReadOnlyList<StepDefinition> steps)
    {
        var newTitle = Text.Required(title, TitleMaxLength, ChoreErrorCodes.TitleInvalid, "A Chore");
        var newDescription = Text.Optional(description, DescriptionMaxLength, ChoreErrorCodes.DescriptionTooLong, "A Chore");

        if (pointMultiplier is < MinPointMultiplier or > MaxPointMultiplier)
        {
            throw new DomainRuleViolation(
                ChoreErrorCodes.MultiplierOutOfRange,
                $"The Point Multiplier must be a whole number from {MinPointMultiplier} to {MaxPointMultiplier}.");
        }

        switch (steps.Count)
        {
            case 0:
                throw new DomainRuleViolation(ChoreErrorCodes.NoSteps, "A Chore needs at least one Step.");
            case > MaxSteps:
                throw new DomainRuleViolation(ChoreErrorCodes.TooManySteps, $"A Chore can have at most {MaxSteps} Steps.");
        }

        // Build every Step before changing anything, so a rejected revision leaves the Chore as it was.
        var newSteps = steps.Select((definition, position) => Step.Create(FamilyId, Id, position, definition)).ToList();

        Title = newTitle;
        Description = newDescription;
        PointMultiplier = pointMultiplier;
        _steps.Clear();
        _steps.AddRange(newSteps);
    }

#pragma warning disable CS8618 // Required by EF Core.
    private Chore()
    {
    }
#pragma warning restore CS8618
}
