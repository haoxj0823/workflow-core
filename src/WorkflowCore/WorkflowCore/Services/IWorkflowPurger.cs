using WorkflowCore.Models;

namespace WorkflowCore.Services;

public interface IWorkflowPurger
{
    Task PurgeWorkflows(WorkflowStatus status, DateTime olderThan, CancellationToken cancellationToken = default);
}