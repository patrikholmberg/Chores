using System.Reflection;
using NetArchTest.Rules;

namespace Chores.Architecture.Tests;

public class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Domain.AssemblyMarker).Assembly;
    private static readonly Assembly Application = typeof(Application.AssemblyMarker).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.AssemblyMarker).Assembly;
    private static readonly Assembly Api = Assembly.Load("Chores.Api");
    private static readonly Assembly Worker = Assembly.Load("Chores.Worker");

    [Fact]
    public void Domain_depends_on_no_other_layer_or_framework()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Chores.Application",
                "Chores.Infrastructure",
                "Chores.Api",
                "Chores.Worker",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Mediator")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Application_depends_only_on_Domain()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Chores.Infrastructure",
                "Chores.Api",
                "Chores.Worker",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_hosts()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny("Chores.Api", "Chores.Worker")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Api_and_Worker_are_independent()
    {
        Assert.DoesNotContain(Api.GetReferencedAssemblies(), a => a.Name == "Chores.Worker");
        Assert.DoesNotContain(Worker.GetReferencedAssemblies(), a => a.Name == "Chores.Api");
    }

    private static void AssertSuccessful(NetArchTest.Rules.TestResult result) =>
        Assert.True(
            result.IsSuccessful,
            "Layer violation in: " + string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
}
