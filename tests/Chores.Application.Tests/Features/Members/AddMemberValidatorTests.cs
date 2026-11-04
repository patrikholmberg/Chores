using Chores.Application.Common.Validation;
using Chores.Application.Features.Members;
using Chores.Domain.Families;

namespace Chores.Application.Tests.Features.Members;

public class AddMemberValidatorTests
{
    private static readonly AddMemberCommand Child = new("nova", "Nova", Role.Child, null, "sv", "abcd");
    private static readonly AddMemberCommand Parent = new("sam", null, Role.Parent, "sam@example.com", "en", "long-enough-pw");

    private static IEnumerable<string> CodesFor(AddMemberCommand command, string property) =>
        new AddMemberValidator().Validate(command).Errors.Where(e => e.PropertyName == property).Select(e => e.ErrorCode);

    [Fact]
    public void A_child_with_a_four_character_password_is_valid() =>
        Assert.True(new AddMemberValidator().Validate(Child).IsValid);

    [Fact]
    public void A_parent_with_email_and_ten_character_password_is_valid() =>
        Assert.True(new AddMemberValidator().Validate(Parent).IsValid);

    [Fact]
    public void A_child_password_needs_four_characters() =>
        Assert.Contains(ValidationCodes.PasswordTooShort, CodesFor(Child with { Password = "abc" }, "Password"));

    [Fact]
    public void A_parent_password_needs_ten_characters() =>
        Assert.Contains(ValidationCodes.PasswordTooShort, CodesFor(Parent with { Password = "abcdefghi" }, "Password"));

    [Fact]
    public void A_parent_needs_an_email() =>
        Assert.Contains(ValidationCodes.Required, CodesFor(Parent with { Email = null }, "Email"));

    [Fact]
    public void A_parent_email_must_be_valid() =>
        Assert.Contains(ValidationCodes.EmailInvalid, CodesFor(Parent with { Email = "nope" }, "Email"));

    [Fact]
    public void A_child_cannot_have_an_email() =>
        Assert.Contains(ValidationCodes.EmailNotAllowed, CodesFor(Child with { Email = "nova@example.com" }, "Email"));

    [Theory]
    [InlineData("big alex")]
    [InlineData("ab")]
    public void The_username_format_is_checked(string username) =>
        Assert.Contains(ValidationCodes.UsernameInvalid, CodesFor(Child with { Username = username }, "Username"));

    [Fact]
    public void The_role_must_be_known() =>
        Assert.Contains(ValidationCodes.RoleInvalid, CodesFor(Child with { Role = (Role)42 }, "Role"));
}
