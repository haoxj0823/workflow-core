using WorkflowCore.Models;
using WorkflowCore.Services.Persistence;

namespace WorkflowCore.Services.DefaultProviders;

public class TransientMemoryPersistenceProvider : IPersistenceProvider
{
    private readonly ISingletonMemoryProvider _innerService;

    public bool SupportsScheduledCommands => false;

    public TransientMemoryPersistenceProvider(ISingletonMemoryProvider innerService)
    {
        _innerService = innerService;
    }

    public Task<string> CreateEventAsync(Event newEvent, CancellationToken _ = default) => _innerService.CreateEventAsync(newEvent);

    public Task<string> CreateEventSubscriptionAsync(EventSubscription subscription, CancellationToken _ = default) => _innerService.CreateEventSubscriptionAsync(subscription);

    public Task<string> CreateNewWorkflowAsync(WorkflowInstance workflow, CancellationToken _ = default) => _innerService.CreateNewWorkflowAsync(workflow);

    public void EnsureStoreExists() => _innerService.EnsureStoreExists();

    public Task<Event> GetEventAsync(string id, CancellationToken _ = default) => _innerService.GetEventAsync(id);

    public Task<IEnumerable<string>> GetEventsAsync(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default) => _innerService.GetEventsAsync(eventName, eventKey, asOf);

    public Task<IEnumerable<string>> GetRunnableEventsAsync(DateTime asAt, CancellationToken _ = default) => _innerService.GetRunnableEventsAsync(asAt);

    public Task<IEnumerable<string>> GetRunnableInstancesAsync(DateTime asAt, CancellationToken _ = default) => _innerService.GetRunnableInstancesAsync(asAt);

    public Task<IEnumerable<EventSubscription>> GetSubscriptionsAsync(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default) => _innerService.GetSubscriptionsAsync(eventName, eventKey, asOf);

    public Task<WorkflowInstance> GetWorkflowInstanceAsync(string Id, CancellationToken _ = default) => _innerService.GetWorkflowInstanceAsync(Id);

    public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesAsync(IEnumerable<string> ids, CancellationToken _ = default) => _innerService.GetWorkflowInstancesAsync(ids);

    public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesAsync(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take) => _innerService.GetWorkflowInstancesAsync(status, type, createdFrom, createdTo, skip, take);

    public Task MarkEventProcessedAsync(string id, CancellationToken _ = default) => _innerService.MarkEventProcessedAsync(id);

    public Task MarkEventUnprocessedAsync(string id, CancellationToken _ = default) => _innerService.MarkEventUnprocessedAsync(id);

    public Task PersistErrorsAsync(IEnumerable<ExecutionError> errors, CancellationToken _ = default) => _innerService.PersistErrorsAsync(errors);

    public Task PersistWorkflowAsync(WorkflowInstance workflow, CancellationToken _ = default) => _innerService.PersistWorkflowAsync(workflow);

    public async Task PersistWorkflowAsync(WorkflowInstance workflow, List<EventSubscription> subscriptions, CancellationToken cancellationToken = default)
    {
        await PersistWorkflowAsync(workflow, cancellationToken);

        foreach(var subscription in subscriptions)
        {
            await CreateEventSubscriptionAsync(subscription, cancellationToken);
        }
    }

    public Task TerminateSubscriptionAsync(string eventSubscriptionId, CancellationToken _ = default) => _innerService.TerminateSubscriptionAsync(eventSubscriptionId);
    public Task<EventSubscription> GetSubscriptionAsync(string eventSubscriptionId, CancellationToken _ = default) => _innerService.GetSubscriptionAsync(eventSubscriptionId);

    public Task<EventSubscription> GetFirstOpenSubscriptionAsync(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default) => _innerService.GetFirstOpenSubscriptionAsync(eventName, eventKey, asOf);

    public Task<bool> SetSubscriptionTokenAsync(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken _ = default) => _innerService.SetSubscriptionTokenAsync(eventSubscriptionId, token, workerId, expiry);

    public Task ClearSubscriptionTokenAsync(string eventSubscriptionId, string token, CancellationToken _ = default) => _innerService.ClearSubscriptionTokenAsync(eventSubscriptionId, token);

    public Task ScheduleCommandAsync(ScheduledCommand command)
    {
        throw new NotImplementedException();
    }

    public Task ProcessCommandsAsync(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
