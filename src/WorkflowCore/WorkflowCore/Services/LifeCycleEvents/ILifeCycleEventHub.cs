using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services.LifeCycleEvents;

public interface ILifeCycleEventHub
{
    Task PublishNotificationAsync(LifeCycleEvent evt, CancellationToken cancellationToken = default);

    void Subscribe(Action<LifeCycleEvent> action);

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
