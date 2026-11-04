using Chores.Application.Common.Exceptions;
using Chores.Application.Features.ChoreLibrary;
using Chores.Application.Tests.Common;
using Chores.Domain.Families;

namespace Chores.Application.Tests.Features.ChoreLibrary;

public class ChoreQueryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_child_cannot_get_an_archived_chore()
    {
        var chore = ChoreTestData.NewChore();
        chore.Archive(DateTimeOffset.UtcNow);
        var handler = new GetChoreHandler(new FakeChoreRepository(chore), new FakeCurrentMember(Role.Child, ChoreTestData.FamilyId));

        await Assert.ThrowsAsync<NotFoundException>(async () => await handler.Handle(new GetChoreQuery(chore.Id), Ct));
    }

    [Fact]
    public async Task A_parent_gets_an_archived_chore_marked_as_archived()
    {
        var chore = ChoreTestData.NewChore();
        chore.Archive(DateTimeOffset.UtcNow);
        var handler = new GetChoreHandler(new FakeChoreRepository(chore), new FakeCurrentMember(Role.Parent, ChoreTestData.FamilyId));

        var dto = await handler.Handle(new GetChoreQuery(chore.Id), Ct);

        Assert.True(dto.Archived);
    }

    [Fact]
    public async Task An_unknown_chore_is_not_found()
    {
        var handler = new GetChoreHandler(new FakeChoreRepository(), new FakeCurrentMember(Role.Parent, ChoreTestData.FamilyId));

        await Assert.ThrowsAsync<NotFoundException>(async () => await handler.Handle(new GetChoreQuery(Guid.NewGuid()), Ct));
    }

    [Theory]
    [InlineData("sv", new[] { "Zebra", "Äpplen", "Ölburkar" })]
    [InlineData("en", new[] { "Äpplen", "Ölburkar", "Zebra" })]
    public async Task The_library_is_sorted_in_the_members_language(string language, string[] expected)
    {
        var (family, parentId) = ChoreTestData.FamilyWithParent(PreferredLanguage.Create(language));
        var repository = new FakeChoreRepository(
            ChoreTestData.NewChore("Ölburkar"), ChoreTestData.NewChore("Zebra"), ChoreTestData.NewChore("Äpplen"));
        var handler = new ListChoresHandler(
            repository, new FakeFamilyRepository(family), new FakeCurrentMember(Role.Parent, ChoreTestData.FamilyId, parentId));

        var titles = (await handler.Handle(new ListChoresQuery(), Ct)).Select(c => c.Title);

        Assert.Equal(expected, titles);
    }

    [Fact]
    public async Task The_library_leaves_out_archived_chores_and_the_archive_list_has_only_them()
    {
        var (family, parentId) = ChoreTestData.FamilyWithParent(PreferredLanguage.English);
        var kept = ChoreTestData.NewChore("Kept");
        var archived = ChoreTestData.NewChore("Archived");
        archived.Archive(DateTimeOffset.UtcNow);
        var repository = new FakeChoreRepository(kept, archived);
        var families = new FakeFamilyRepository(family);
        var parent = new FakeCurrentMember(Role.Parent, ChoreTestData.FamilyId, parentId);

        var library = await new ListChoresHandler(repository, families, parent).Handle(new ListChoresQuery(), Ct);
        var archive = await new ListArchivedChoresHandler(repository, families, parent).Handle(new ListArchivedChoresQuery(), Ct);

        Assert.Equal(["Kept"], library.Select(c => c.Title));
        Assert.Equal(["Archived"], archive.Select(c => c.Title));
        Assert.Equal(1, library[0].StepCount);
    }
}
