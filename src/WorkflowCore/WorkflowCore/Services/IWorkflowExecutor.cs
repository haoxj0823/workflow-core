using WorkflowCore.Models;

namespace WorkflowCore.Services;

public interface IWorkflowExecutor
{
    Task<WorkflowExecutorResult> Execute(WorkflowInstance workflow, CancellationToken cancellationToken = default);
}