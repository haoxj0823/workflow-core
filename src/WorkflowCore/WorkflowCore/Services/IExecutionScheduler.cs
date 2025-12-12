using WorkflowCore.Models;

namespace WorkflowCore.Services;

public interface IExecutionScheduler
{
    Task DetermineNextExecutionTimeAsync(WorkflowInstance workflow, WorkflowDefinition def, CancellationToken cancellationToken = default);
}
