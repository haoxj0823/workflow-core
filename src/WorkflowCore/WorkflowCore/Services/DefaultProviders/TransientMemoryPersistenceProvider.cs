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

    public Task<string> CreateEventAsync(Event newEvent, CancellationToken cancellationToken = default) => _innerService.CreateEventAsync(newEvent, cancellationToken);

    public Task<string> CreateEventSubscriptionAsync(EventSubscription subscription, CancellationToken cancellationToken = default) => _innerService.CreateEventSubscriptionAsync(subscription, cancellationToken);

    public Task<string> CreateNewWorkflowAsync(WorkflowInstance workflow, CancellationToken cancellationToken = default) => _innerService.CreateNewWorkflowAsync(workflow, cancellationToken);

    public void EnsureStoreExists() => _innerService.EnsureStoreExists();

    public Task<Event> GetEventAsync(string id, CancellationToken cancellationToken = default) => _innerService.GetEventAsync(id, cancellationToken);

    public Task<IEnumerable<string>> GetEventsAsync(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default) => _innerService.GetEventsAsync(eventName, eventKey, asOf, cancellationToken);

    public Task<IEnumerable<string>> GetRunnableEventsAsync(DateTime asAt, CancellationToken cancellationToken = default) => _innerService.GetRunnableEventsAsync(asAt, cancellationToken);

    public Task<IEnumerable<string>> GetRunnableInstancesAsync(DateTime asAt, CancellationToken cancellationToken = default) => _innerService.GetRunnableInstancesAsync(asAt, cancellationToken);

    public Task<IEnumerable<EventSubscription>> GetSubscriptionsAsync(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default) => _innerService.GetSubscriptionsAsync(eventName, eventKey, asOf, cancellationToken);

    public Task<WorkflowInstance> GetWorkflowInstanceAsync(string Id, CancellationToken cancellationToken = default) => _innerService.GetWorkflowInstanceAsync(Id, cancellationToken);

    public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => _innerService.GetWorkflowInstancesAsync(ids, cancellationToken);

    public Task MarkEventProcessedAsync(string id, CancellationToken cancellationToken = default) => _innerService.MarkEventProcessedAsync(id, cancellationToken);

    public Task MarkEventUnprocessedAsync(string id, CancellationToken cancellationToken = default) => _innerService.MarkEventUnprocessedAsync(id, cancellationToken);

    public Task PersistErrorsAsync(IEnumerable<ExecutionError> errors, CancellationToken cancellationToken = default) => _innerService.PersistErrorsAsync(errors, cancellationToken);

    public Task PersistWorkflowAsync(WorkflowInstance workflow, CancellationToken cancellationToken = default) => _innerService.PersistWorkflowAsync(workflow, cancellationToken);

    public async Task PersistWorkflowAsync(WorkflowInstance workflow, List<EventSubscription> subscriptions, CancellationToken cancellationToken = default)
    {
        await PersistWorkflowAsync(workflow, cancellationToken);

        foreach (var subscription in subscriptions)
        {
            await CreateEventSubscriptionAsync(subscription, cancellationToken);
        }
    }

    public Task TerminateSubscriptionAsync(string eventSubscriptionId, CancellationToken cancellationToken = default) => _innerService.TerminateSubscriptionAsync(eventSubscriptionId, cancellationToken);

    public Task<EventSubscription> GetSubscriptionAsync(string eventSubscriptionId, CancellationToken cancellationToken = default) => _innerService.GetSubscriptionAsync(eventSubscriptionId, cancellationToken);

    public Task<EventSubscription> GetFirstOpenSubscriptionAsync(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default) => _innerService.GetFirstOpenSubscriptionAsync(eventName, eventKey, asOf, cancellationToken);

    public Task<bool> SetSubscriptionTokenAsync(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken cancellationToken = default) => _innerService.SetSubscriptionTokenAsync(eventSubscriptionId, token, workerId, expiry, cancellationToken);

    public Task ClearSubscriptionTokenAsync(string eventSubscriptionId, string token, CancellationToken cancellationToken = default) => _innerService.ClearSubscriptionTokenAsync(eventSubscriptionId, token, cancellationToken);

    public Task ScheduleCommandAsync(ScheduledCommand command, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task ProcessCommandsAsync(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
