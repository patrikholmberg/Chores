using Chores.Application.Common.Identity;
using Chores.Application.Common.Persistence;
using Chores.Application.Common.Tenancy;
using Chores.Application.Common.Validation;
using Chores.Domain.Families;
using FluentValidation;
using Mediator;

namespace Chores.Application.Features.Members;

/// <summary>A Parent adds a Parent or Child to their own Family, with an initial password.</summary>
public sealed record AddMemberCommand(
    string Username,
    string? DisplayName,
    Role Role,
    string? Email,
    string Language,
    string Password) : ICommand<MemberDto>, IParentOnly;

public sealed class AddMemberValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberValidator()
    {
        RuleFor(c => c.Username).ValidUsername();
        RuleFor(c => c.DisplayName).DisplayName();
        RuleFor(c => c.Role).IsInEnum().WithErrorCode(ValidationCodes.RoleInvalid);
        RuleFor(c => c.Email).EmailForRole(c => c.Role);
        RuleFor(c => c.Language).ValidLanguage();
        RuleFor(c => c.Password).PasswordFor(c => c.Role);
    }
}

public sealed class AddMemberHandler(
    IFamilyRepository families,
    IMemberCredentials credentials,
    IUnitOfWork unitOfWork) : ICommandHandler<AddMemberCommand, MemberDto>
{
    public async ValueTask<MemberDto> Handle(AddMemberCommand command, CancellationToken cancellationToken)
    {
        var family = await families.GetRequiredAsync(cancellationToken);
        var username = Username.Create(command.Username);
        if (await credentials.IsUsernameTakenAsync(username, null, cancellationToken))
        {
            throw ValidationFailures.UsernameTaken();
        }

        var member = family.AddMember(
            Guid.CreateVersion7(),
            username,
            command.DisplayName,
            command.Role,
            command.Email,
            PreferredLanguage.Create(command.Language));
        await credentials.CreateAsync(member.Id, username, member.Email, command.Password, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MemberDto.From(member);
    }
}
