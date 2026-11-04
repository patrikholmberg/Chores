using Chores.Domain.Common;
using Chores.Domain.Families;

namespace Chores.Domain.Tests.Families;

public class FamilyTests
{
    private static readonly Guid ParentId = Guid.NewGuid();

    private static Family NewFamily() => Family.Register(
        Guid.NewGuid(),
        "  The Holmbergs ",
        FamilyTimeZone.Create("Europe/Stockholm"),
        ParentId,
        Username.Create("alex"),
        "Alex",
        "alex@example.com",
        PreferredLanguage.Swedish);

    private static Member AddChild(Family family, string username = "nova") =>
        family.AddMember(Guid.NewGuid(), Username.Create(username), null, Role.Child, null, PreferredLanguage.Swedish);

    private static Member AddParent(Family family, string username = "sam") =>
        family.AddMember(Guid.NewGuid(), Username.Create(username), null, Role.Parent, "sam@example.com", PreferredLanguage.English);

    private static void AssertViolation(string code, Action action) =>
        Assert.Equal(code, Assert.Throws<DomainRuleViolation>(action).Code);

    [Fact]
    public void Register_creates_the_first_parent()
    {
        var family = NewFamily();

        Assert.Equal("The Holmbergs", family.Name);
        var parent = Assert.Single(family.Members);
        Assert.Equal(Role.Parent, parent.Role);
        Assert.Equal(family.Id, parent.FamilyId);
        Assert.Equal("alex@example.com", parent.Email);
    }

    [Fact]
    public void Display_name_defaults_to_the_username()
    {
        var child = AddChild(NewFamily());

        Assert.Equal("nova", child.DisplayName);
    }

    [Fact]
    public void A_parent_needs_an_email()
    {
        var family = NewFamily();

        AssertViolation(FamilyErrorCodes.EmailRequired, () =>
            family.AddMember(Guid.NewGuid(), Username.Create("sam"), null, Role.Parent, " ", PreferredLanguage.English));
    }

    [Fact]
    public void A_child_cannot_have_an_email()
    {
        var family = NewFamily();

        AssertViolation(FamilyErrorCodes.EmailNotAllowed, () =>
            family.AddMember(Guid.NewGuid(), Username.Create("nova"), null, Role.Child, "nova@example.com", PreferredLanguage.English));
    }

    [Fact]
    public void Removing_the_only_parent_is_refused()
    {
        var family = NewFamily();
        AddChild(family);

        AssertViolation(FamilyErrorCodes.LastParent, () => family.RemoveMember(ParentId, DateTimeOffset.UtcNow));
        Assert.Equal(2, family.Members.Count);
    }

    [Fact]
    public void Demoting_the_only_parent_is_refused()
    {
        var family = NewFamily();

        AssertViolation(FamilyErrorCodes.LastParent, () => family.ChangeRole(ParentId, Role.Child, null));
        Assert.Equal(Role.Parent, family.Members.Single().Role);
    }

    [Fact]
    public void Demoting_one_of_two_parents_is_allowed_and_clears_the_email()
    {
        var family = NewFamily();
        var sam = AddParent(family);

        family.ChangeRole(sam.Id, Role.Child, null);

        Assert.Equal(Role.Child, sam.Role);
        Assert.Null(sam.Email);
        Assert.Single(family.Members, m => m.Role == Role.Parent);
    }

    [Fact]
    public void Demoting_with_an_email_is_refused()
    {
        var family = NewFamily();
        var sam = AddParent(family);

        AssertViolation(FamilyErrorCodes.EmailNotAllowed, () => family.ChangeRole(sam.Id, Role.Child, "sam@example.com"));
    }

    [Fact]
    public void Promoting_a_child_requires_an_email()
    {
        var family = NewFamily();
        var nova = AddChild(family);

        AssertViolation(FamilyErrorCodes.EmailRequired, () => family.ChangeRole(nova.Id, Role.Parent, null));

        family.ChangeRole(nova.Id, Role.Parent, "nova@example.com");
        Assert.Equal(Role.Parent, nova.Role);
    }

    [Fact]
    public void Removing_a_child_hides_it_from_members()
    {
        var family = NewFamily();
        var nova = AddChild(family);

        family.RemoveMember(nova.Id, DateTimeOffset.UtcNow);

        Assert.True(nova.IsRemoved);
        Assert.DoesNotContain(nova, family.Members);
    }

    [Fact]
    public void Removing_one_of_two_parents_is_allowed_then_the_last_is_protected()
    {
        var family = NewFamily();
        var sam = AddParent(family);

        family.RemoveMember(ParentId, DateTimeOffset.UtcNow);

        AssertViolation(FamilyErrorCodes.LastParent, () => family.RemoveMember(sam.Id, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Role_changes_and_removals_bump_the_revision()
    {
        var family = NewFamily();
        var nova = AddChild(family);

        family.ChangeRole(nova.Id, Role.Parent, "nova@example.com");
        family.RemoveMember(nova.Id, DateTimeOffset.UtcNow);

        Assert.Equal(2, family.Revision);
    }

    [Fact]
    public void Operating_on_an_unknown_member_is_refused()
    {
        var family = NewFamily();

        AssertViolation(FamilyErrorCodes.MemberNotFound, () => family.RemoveMember(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Update_changes_username_display_name_and_language()
    {
        var family = NewFamily();
        var nova = AddChild(family);

        family.UpdateMember(nova.Id, Username.Create("Nova2"), " Nova ", PreferredLanguage.English);

        Assert.Equal("nova2", nova.Username.Value);
        Assert.Equal("Nova", nova.DisplayName);
        Assert.Equal(PreferredLanguage.English, nova.PreferredLanguage);
    }
}
