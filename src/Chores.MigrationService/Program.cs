using Chores.Infrastructure;
using Chores.Infrastructure.Identity;
using Chores.MigrationService;

// `reset-password <username>` reads the new password from stdin; anything else migrates the database.
var resetUsername = args is ["reset-password", var name] ? name : null;

var builder = Host.CreateApplicationBuilder(resetUsername is null ? args : []);

builder.AddServiceDefaults();
builder.AddInfrastructure();

if (resetUsername is not null)
{
    using var host = builder.Build();
    await using var scope = host.Services.CreateAsyncScope();

    var password = Console.In.ReadLine() ?? string.Empty;
    var error = await scope.ServiceProvider.GetRequiredService<OperatorPasswordReset>()
        .ResetAsync(resetUsername, password, CancellationToken.None);

    Console.Error.WriteLine(error ?? $"Password for '{resetUsername}' was reset and their sessions ended.");
    return error is null ? 0 : 1;
}

builder.Services.AddHostedService<MigrationWorker>();
builder.Build().Run();
return Environment.ExitCode;
