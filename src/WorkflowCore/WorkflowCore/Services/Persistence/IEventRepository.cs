using WorkflowCore.Models;

namespace WorkflowCore.Services.Persistence;

public interface IEventRepository
{
    Task<string> CreateEventAsync(Event newEvent, CancellationToken cancellationToken = default);

    Task<Event> GetEventAsync(string id, CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetRunnableEventsAsync(DateTime asAt, CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetEventsAsync(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default);

    Task MarkEventProcessedAsync(string id, CancellationToken cancellationToken = default);

    Task MarkEventUnprocessedAsync(string id, CancellationToken cancellationToken = default);
}
