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

var web = builder.AddViteApp("web", "../web")
    .WithEnvironment("API_HTTP", api.GetEndpoint("http"))
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsPublishMode)
{
    // Behind the HTTPS reverse proxy the web app's public URL is the token issuer and the OAuth redirect origin.
    var publicUrl = builder.AddParameter("public-url");
    api.WithEnvironment("Auth__Issuer", publicUrl)
        .WithEnvironment("Auth__SigningCertificate", builder.AddParameter("auth-signing-certificate", secret: true))
        .WithEnvironment("Auth__SigningCertificatePassword", builder.AddParameter("auth-signing-certificate-password", secret: true))
        .WithEnvironment("Auth__EncryptionCertificate", builder.AddParameter("auth-encryption-certificate", secret: true))
        .WithEnvironment("Auth__EncryptionCertificatePassword", builder.AddParameter("auth-encryption-certificate-password", secret: true))
        .WithEnvironment("Captcha__HmacKey", builder.AddParameter("captcha-hmac-key", secret: true));
    migrations.WithEnvironment("Auth__WebOrigins__0", publicUrl);
}
else
{
    // Locally the browser talks to Vite, which proxies /api and /connect, so Vite's origin is the issuer.
    api.WithEnvironment("Auth__Issuer", web.GetEndpoint("http"));
    migrations.WithEnvironment("Auth__WebOrigins__0", web.GetEndpoint("http"));
}

builder.Build().Run();
