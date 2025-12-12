using WorkflowCore.Models;

namespace WorkflowCore.Services.Executors;

public interface IWorkflowExecutor
{
    Task<WorkflowExecutorResult> ExecuteAsync(WorkflowInstance workflow, CancellationToken cancellationToken = default);
}