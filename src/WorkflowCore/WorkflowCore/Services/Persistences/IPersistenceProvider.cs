using WorkflowCore.Models;

namespace WorkflowCore.Services.Persistences;

public interface IPersistenceProvider : IWorkflowRepository, ISubscriptionRepository, IEventRepository, IScheduledCommandRepository
{        
    Task PersistErrorsAsync(IEnumerable<ExecutionError> errors, CancellationToken cancellationToken = default);

    void EnsureStoreExists();
}
