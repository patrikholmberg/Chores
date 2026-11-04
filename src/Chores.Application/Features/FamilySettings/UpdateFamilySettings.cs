using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Common.Validation;
using Chores.Application.Features.Members;
using Chores.Domain.Families;
using FluentValidation;
using Mediator;

namespace Chores.Application.Features.FamilySettings;

/// <summary>A Parent changes the Family's settings. They apply from the moment they are saved; the last save wins.</summary>
public sealed record UpdateFamilySettingsCommand(int BasePoints, int AttemptWindowDays) : ICommand<FamilySettingsDto>, IParentOnly;

public sealed class UpdateFamilySettingsValidator : AbstractValidator<UpdateFamilySettingsCommand>
{
    public UpdateFamilySettingsValidator()
    {
        RuleFor(c => c.BasePoints)
            .InclusiveBetween(Family.MinBasePoints, Family.MaxBasePoints)
            .WithErrorCode(ValidationCodes.BasePointsOutOfRange)
            .WithMessage($"Base Points must be a whole number from {Family.MinBasePoints} to {Family.MaxBasePoints}.");
        RuleFor(c => c.AttemptWindowDays)
            .InclusiveBetween(Family.MinAttemptWindowDays, Family.MaxAttemptWindowDays)
            .WithErrorCode(ValidationCodes.AttemptWindowOutOfRange)
            .WithMessage($"The attempt window must be {Family.MinAttemptWindowDays} to {Family.MaxAttemptWindowDays} days.");
    }
}

public sealed class UpdateFamilySettingsHandler(IFamilyRepository families, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateFamilySettingsCommand, FamilySettingsDto>
{
    public async ValueTask<FamilySettingsDto> Handle(UpdateFamilySettingsCommand command, CancellationToken cancellationToken)
    {
        var family = await families.GetRequiredAsync(cancellationToken);
        family.ChangeSettings(command.BasePoints, command.AttemptWindowDays);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return FamilySettingsDto.From(family);
    }
}
