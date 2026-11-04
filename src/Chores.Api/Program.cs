using System.Text.Json;
using System.Text.Json.Serialization;
using Chores.Api.Auth;
using Chores.Api.Endpoints;
using Chores.Api.Infrastructure;
using Chores.Application;
using Chores.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddInfrastructure();
builder.Services.AddApplication();
builder.AddChoresAuth();

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddChoresRateLimiting(builder.Configuration);
builder.Services.AddSingleton<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapServerInfoEndpoints();
app.MapCaptchaEndpoints();
app.MapFamilyEndpoints();
app.MapAccountEndpoints();
app.MapConnectEndpoints();
app.MapMeEndpoints();
app.MapMemberEndpoints();
app.MapChoreEndpoints();

app.Run();

public partial class Program;
