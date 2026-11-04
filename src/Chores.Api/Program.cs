using Chores.Api.Endpoints;
using Chores.Api.Infrastructure;
using Chores.Application;
using Chores.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddInfrastructure();
builder.Services.AddApplication();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();
app.MapServerInfoEndpoints();

app.Run();

public partial class Program;
