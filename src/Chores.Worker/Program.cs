using Chores.Application;
using Chores.Infrastructure;
using Chores.Worker.Jobs;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddInfrastructure();
builder.Services.AddApplication();

var connectionString = builder.Configuration.GetConnectionString(Chores.Infrastructure.DependencyInjection.DatabaseName)
    ?? throw new InvalidOperationException("Connection string for the Quartz job store is missing.");

builder.Services.AddQuartz(quartz =>
{
    quartz.ConfigureScheduler(scheduler =>
    {
        scheduler.InstanceName = "chores";
        scheduler.GenerateInstanceId = true;
    });

    quartz.UsePersistentStore(store =>
    {
        store.UsePostgres(connectionString);
        store.ConfigureStore(options => options.TablePrefix = "quartz.qrtz_");
        store.UseClustering();
        store.UseSystemTextJsonSerializer();
    });

    quartz.ScheduleJob<HeartbeatJob>(
        trigger => trigger.WithIdentity("heartbeat-every-minute").WithCronSchedule("0 * * * * ?"),
        job => job.WithIdentity(HeartbeatJob.Key));
});

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
