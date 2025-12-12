using WorkflowCore.Models;

namespace WorkflowCore.Services.Persistence;

public interface IPersistenceProvider : IWorkflowRepository, ISubscriptionRepository, IEventRepository, IScheduledCommandRepository
{        
    Task PersistErrorsAsync(IEnumerable<ExecutionError> errors, CancellationToken cancellationToken = default);

    void EnsureStoreExists();
}
