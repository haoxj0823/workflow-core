using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Services.LifeCycleEvents;
using WorkflowCore.Services.Middleware;

namespace WorkflowCore.Services;

public class ExecutionScheduler : IExecutionScheduler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDateTimeProvider _datetimeProvider;
    private readonly ILifeCycleEventPublisher _publisher;

    public ExecutionScheduler(
        IServiceProvider serviceProvider,
        IDateTimeProvider datetimeProvider,
        ILifeCycleEventPublisher publisher)
    {
        _serviceProvider = serviceProvider;
        _datetimeProvider = datetimeProvider;
        _publisher = publisher;
    }

    public async Task DetermineNextExecutionTimeAsync(WorkflowInstance workflow, WorkflowDefinition def, CancellationToken cancellationToken = default)
    {
        workflow.NextExecution = null;

        if (workflow.Status == WorkflowStatus.Complete)
        {
            return;
        }

        foreach (var pointer in workflow.ExecutionPointers.Where(x => x.Active && (x.Children ?? []).Count == 0))
        {
            if (!pointer.SleepUntil.HasValue)
            {
                workflow.NextExecution = 0;
                return;
            }

            var pointerSleep = pointer.SleepUntil.Value.ToUniversalTime().Ticks;
            workflow.NextExecution = Math.Min(pointerSleep, workflow.NextExecution ?? pointerSleep);
        }

        foreach (var pointer in workflow.ExecutionPointers.Where(x => x.Active && (x.Children ?? []).Count > 0))
        {
            if (!workflow.ExecutionPointers.FindByScope(pointer.Id).All(x => x.EndTime.HasValue))
            {
                continue;
            }

            if (!pointer.SleepUntil.HasValue)
            {
                workflow.NextExecution = 0;
                return;
            }

            var pointerSleep = pointer.SleepUntil.Value.ToUniversalTime().Ticks;
            workflow.NextExecution = Math.Min(pointerSleep, workflow.NextExecution ?? pointerSleep);
        }

        if ((workflow.NextExecution != null) || (workflow.ExecutionPointers.Any(x => x.EndTime == null)))
        {
            return;
        }

        workflow.Status = WorkflowStatus.Complete;
        workflow.CompleteTime = _datetimeProvider.UtcNow;

        using (var scope = _serviceProvider.CreateScope())
        {
            var middlewareRunner = scope.ServiceProvider.GetRequiredService<IWorkflowMiddlewareRunner>();
            await middlewareRunner.RunPostMiddlewareAsync(workflow, def,cancellationToken);
        }

        _publisher.PublishNotification(new WorkflowCompleted
        {
            EventTimeUtc = _datetimeProvider.UtcNow,
            Reference = workflow.Reference,
            WorkflowInstanceId = workflow.Id,
            WorkflowDefinitionId = workflow.WorkflowDefinitionId,
            Version = workflow.Version
        });
    }
}