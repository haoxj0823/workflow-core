using WorkflowCore.Models;

namespace WorkflowCore.Services;

public interface IWorkflowPurger
{
    Task PurgeWorkflowsAsync(WorkflowStatus status, DateTime olderThan, CancellationToken cancellationToken = default);
}