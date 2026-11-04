using Chores.Infrastructure;
using Chores.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddInfrastructure();
builder.Services.AddHostedService<MigrationWorker>();

builder.Build().Run();
