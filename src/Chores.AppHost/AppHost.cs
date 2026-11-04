var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("chores");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("choresdb");

var migrations = builder.AddProject<Projects.Chores_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

var api = builder.AddProject<Projects.Chores_Api>("api")
    .WithReference(database)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Chores_Worker>("worker")
    .WithReference(database)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");

builder.AddViteApp("web", "../web")
    .WithEnvironment("API_HTTP", api.GetEndpoint("http"))
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
