using System.Net;
using System.Net.Http.Json;
using Chores.Domain.Common;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests;

public class ApiPipelineTests(UnreachableDatabaseFactory factory) : IClassFixture<UnreachableDatabaseFactory>
{
    [Fact]
    public async Task Info_returns_the_server_version()
    {
        var info = await factory.CreateClient()
            .GetFromJsonAsync<ServerInfoResponse>("/api/info", TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(info?.Version));
    }

    [Fact]
    public async Task Validation_failure_maps_to_400_problem_details()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter, ThrowingEndpointFilter>()))
            .CreateClient();

        var response = await client.GetAsync("/test/invalid", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Domain_rule_violation_maps_to_409_with_its_code()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter, ThrowingEndpointFilter>()))
            .CreateClient();

        var response = await client.GetAsync("/test/rule", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("member.last_parent", problem!.Extensions["code"]?.ToString());
    }

    private sealed record ServerInfoResponse(string Version);

    /// <summary>Adds endpoints that throw the exceptions the Mediator pipeline would.</summary>
    private sealed class ThrowingEndpointFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/test/invalid", (HttpContext _) =>
                    throw new ValidationException([new ValidationFailure("Name", "'Name' must not be empty.")]));
                endpoints.MapGet("/test/rule", (HttpContext _) =>
                    throw new DomainRuleViolation("member.last_parent", "A Family must keep at least one Parent."));
            });
        };
    }
}
