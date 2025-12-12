using Microsoft.Extensions.Options;
using WorkflowCore.Models;

namespace WorkflowCore.Services.ErrorHandlers;

public class RetryHandler : IWorkflowErrorHandler
{
    private readonly IDateTimeProvider _datetimeProvider;
    private readonly WorkflowOptions _workflowOptions;

    public WorkflowErrorHandling Type => WorkflowErrorHandling.Retry;

    public RetryHandler(
        IDateTimeProvider datetimeProvider,
        IOptions<WorkflowOptions> workflowOptions)
    {
        _datetimeProvider = datetimeProvider;
        _workflowOptions = workflowOptions.Value;
    }

    public void Handle(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, Exception exception, Queue<ExecutionPointer> bubbleUpQueue)
    {
        pointer.RetryCount++;
        pointer.SleepUntil = _datetimeProvider.UtcNow.Add(step.RetryInterval ?? def.DefaultErrorRetryInterval ?? _workflowOptions.ErrorRetryInterval);
        step.PrimeForRetry(pointer);
    }
}
