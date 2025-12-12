using WorkflowCore.Models;
using WorkflowCore.Services.Persistence;

namespace WorkflowCore.Services.DefaultProviders;

public class MemoryPersistenceProvider : ISingletonMemoryProvider
{
    private readonly List<WorkflowInstance> _instances = [];
    private readonly List<EventSubscription> _subscriptions = [];
    private readonly List<Event> _events = [];
    private readonly List<ExecutionError> _errors = [];

    public bool SupportsScheduledCommands => false;

    public Task<string> CreateNewWorkflowAsync(WorkflowInstance workflow, CancellationToken _ = default)
    {
        lock (_instances)
        {
            workflow.Id = Guid.NewGuid().ToString();
            _instances.Add(workflow);
            return Task.FromResult(workflow.Id);
        }
    }

    public Task PersistWorkflowAsync(WorkflowInstance workflow, CancellationToken _ = default)
    {
        lock (_instances)
        {
            var existing = _instances.First(x => x.Id == workflow.Id);
            _instances.Remove(existing);
            _instances.Add(workflow);
            return Task.CompletedTask;
        }
    }

    public Task PersistWorkflowAsync(WorkflowInstance workflow, List<EventSubscription> subscriptions, CancellationToken cancellationToken = default)
    {
        lock (_instances)
        {
            var existing = _instances.First(x => x.Id == workflow.Id);
            _instances.Remove(existing);
            _instances.Add(workflow);
        }

        lock (_subscriptions)
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Id = Guid.NewGuid().ToString();
                _subscriptions.Add(subscription);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetRunnableInstancesAsync(DateTime asAt, CancellationToken _ = default)
    {
        lock (_instances)
        {
            var now = asAt.ToUniversalTime().Ticks;
            var instances = _instances.Where(x => x.NextExecution.HasValue && x.NextExecution <= now).Select(x => x.Id).ToList();
            return Task.FromResult<IEnumerable<string>>(instances);
        }
    }

    public Task<WorkflowInstance> GetWorkflowInstanceAsync(string Id, CancellationToken _ = default)
    {
        lock (_instances)
        {
            var instance = _instances.FirstOrDefault(x => x.Id == Id);
            return Task.FromResult(instance);
        }
    }

    public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesAsync(IEnumerable<string> ids, CancellationToken _ = default)
    {
        if (ids == null)
        {
            return Task.FromResult<IEnumerable<WorkflowInstance>>([]);
        }

        lock (_instances)
        {
            var instances = _instances.Where(x => ids.Contains(x.Id)).ToList();
            return Task.FromResult<IEnumerable<WorkflowInstance>>(instances);
        }
    }

    public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesAsync(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
    {
        lock (_instances)
        {
            var result = _instances.AsQueryable();

            if (status.HasValue)
            {
                result = result.Where(x => x.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(type))
            {
                result = result.Where(x => x.WorkflowDefinitionId == type);
            }

            if (createdFrom.HasValue)
            {
                result = result.Where(x => x.CreateTime >= createdFrom.Value);
            }

            if (createdTo.HasValue)
            {
                result = result.Where(x => x.CreateTime <= createdTo.Value);
            }

            var instances = result.Skip(skip).Take(take).ToList();
            return Task.FromResult<IEnumerable<WorkflowInstance>>(instances);
        }
    }

    public Task<string> CreateEventSubscriptionAsync(EventSubscription subscription, CancellationToken _ = default)
    {
        lock (_subscriptions)
        {
            subscription.Id = Guid.NewGuid().ToString();
            _subscriptions.Add(subscription);
            return Task.FromResult(subscription.Id);
        }
    }

    public Task<IEnumerable<EventSubscription>> GetSubscriptionsAsync(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default)
    {
        lock (_subscriptions)
        {
            var subscriptions = _subscriptions.Where(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf).ToList();
            return Task.FromResult<IEnumerable<EventSubscription>>(subscriptions);
        }
    }

    public Task TerminateSubscriptionAsync(string eventSubscriptionId, CancellationToken _ = default)
    {
        lock (_subscriptions)
        {
            var sub = _subscriptions.Single(x => x.Id == eventSubscriptionId);
            _subscriptions.Remove(sub);

            return Task.CompletedTask;
        }
    }

    public Task<EventSubscription> GetSubscriptionAsync(string eventSubscriptionId, CancellationToken _ = default)
    {
        lock (_subscriptions)
        {
            var sub = _subscriptions.Single(x => x.Id == eventSubscriptionId);
            return Task.FromResult(sub);
        }
    }

    public Task<EventSubscription> GetFirstOpenSubscriptionAsync(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default)
    {
        lock (_subscriptions)
        {
            var result = _subscriptions.FirstOrDefault(x => x.ExternalToken == null && x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf);
            return Task.FromResult(result);
        }
    }

    public Task<bool> SetSubscriptionTokenAsync(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken _ = default)
    {
        lock (_subscriptions)
        {
            var sub = _subscriptions.Single(x => x.Id == eventSubscriptionId);
            sub.ExternalToken = token;
            sub.ExternalWorkerId = workerId;
            sub.ExternalTokenExpiry = expiry;
            return Task.FromResult(true);
        }
    }

    public Task ClearSubscriptionTokenAsync(string eventSubscriptionId, string token, CancellationToken _ = default)
    {
        lock (_subscriptions)
        {
            var sub = _subscriptions.Single(x => x.Id == eventSubscriptionId);
            if (sub.ExternalToken != token)
            {
                throw new InvalidOperationException();
            }

            sub.ExternalToken = null;
            sub.ExternalWorkerId = null;
            sub.ExternalTokenExpiry = null;

            return Task.CompletedTask;
        }
    }

    public void EnsureStoreExists()
    {
    }

    public Task<string> CreateEventAsync(Event newEvent, CancellationToken _ = default)
    {
        lock (_events)
        {
            newEvent.Id = Guid.NewGuid().ToString();
            _events.Add(newEvent);
            return Task.FromResult(newEvent.Id);
        }
    }

    public Task MarkEventProcessedAsync(string id, CancellationToken _ = default)
    {
        lock (_events)
        {
            var evt = _events.FirstOrDefault(x => x.Id == id);
            if (evt != null)
            {
                evt.IsProcessed = true;
            }
            return Task.CompletedTask;
        }
    }

    public Task<IEnumerable<string>> GetRunnableEventsAsync(DateTime asAt, CancellationToken _ = default)
    {
        lock (_events)
        {
            var events = _events
                .Where(x => !x.IsProcessed)
                .Where(x => x.EventTime <= asAt.ToUniversalTime())
                .Select(x => x.Id)
                .ToList();

            return Task.FromResult<IEnumerable<string>>(events);
        }
    }

    public Task<Event> GetEventAsync(string id, CancellationToken _ = default)
    {
        lock (_events)
        {
            return Task.FromResult(_events.FirstOrDefault(x => x.Id == id));
        }
    }

    public Task<IEnumerable<string>> GetEventsAsync(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default)
    {
        lock (_events)
        {
            var events = _events
                .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                .Where(x => x.EventTime >= asOf)
                .Select(x => x.Id)
                .ToList();

            return Task.FromResult<IEnumerable<string>>(events);
        }
    }

    public Task MarkEventUnprocessedAsync(string id, CancellationToken _ = default)
    {
        lock (_events)
        {
            var evt = _events.FirstOrDefault(x => x.Id == id);
            if (evt != null)
            {
                evt.IsProcessed = false;
            }

            return Task.CompletedTask;
        }
    }

    public Task PersistErrorsAsync(IEnumerable<ExecutionError> errors, CancellationToken _ = default)
    {
        lock (_errors)
        {
            _errors.AddRange(errors);
            return Task.CompletedTask;
        }
    }

    public Task ScheduleCommandAsync(ScheduledCommand command)
    {
        throw new NotImplementedException();
    }

    public Task ProcessCommandsAsync(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
