using Quartz;

namespace Chores.Worker.Jobs;

/// <summary>Logs once per minute to prove that clustered scheduling fires each trigger on exactly one Worker.</summary>
[DisallowConcurrentExecution]
public sealed class HeartbeatJob(ILogger<HeartbeatJob> logger) : IJob
{
    public static readonly JobKey Key = new("heartbeat");

    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Heartbeat fired at {FireTime:O} on {SchedulerInstance}",
            context.ScheduledFireTimeUtc,
            context.Scheduler.SchedulerInstanceId);

        return ValueTask.CompletedTask;
    }
}
