using WorkflowCore.Models;

namespace WorkflowCore.Services.Persistence;

public interface IWorkflowRepository
{
    Task<string> CreateNewWorkflowAsync(WorkflowInstance workflow, CancellationToken cancellationToken = default);

    Task PersistWorkflowAsync(WorkflowInstance workflow, CancellationToken cancellationToken = default);

    Task PersistWorkflowAsync(WorkflowInstance workflow, List<EventSubscription> subscriptions, CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetRunnableInstancesAsync(DateTime asAt, CancellationToken cancellationToken = default);

    [Obsolete]
    Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesAsync(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take);

    Task<WorkflowInstance> GetWorkflowInstanceAsync(string Id, CancellationToken cancellationToken = default);

    Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
}
