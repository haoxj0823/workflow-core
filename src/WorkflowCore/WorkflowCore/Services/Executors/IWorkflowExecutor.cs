using WorkflowCore.Models;

namespace WorkflowCore.Services.Executors;

public interface IWorkflowExecutor
{
    Task<WorkflowExecutorResult> Execute(WorkflowInstance workflow, CancellationToken cancellationToken = default);
}