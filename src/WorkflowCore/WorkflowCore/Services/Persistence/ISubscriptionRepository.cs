using WorkflowCore.Models;

namespace WorkflowCore.Services.Persistence;

public interface ISubscriptionRepository
{        
    Task<string> CreateEventSubscriptionAsync(EventSubscription subscription, CancellationToken cancellationToken = default);

    Task<IEnumerable<EventSubscription>> GetSubscriptionsAsync(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default);

    Task TerminateSubscriptionAsync(string eventSubscriptionId, CancellationToken cancellationToken = default);

    Task<EventSubscription> GetSubscriptionAsync(string eventSubscriptionId, CancellationToken cancellationToken = default);

    Task<EventSubscription> GetFirstOpenSubscriptionAsync(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default);
    
    Task<bool> SetSubscriptionTokenAsync(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken cancellationToken = default);
    
    Task ClearSubscriptionTokenAsync(string eventSubscriptionId, string token, CancellationToken cancellationToken = default);
}
