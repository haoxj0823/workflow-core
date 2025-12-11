using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services;

public interface ILifeCycleEventHub
{
    Task PublishNotification(LifeCycleEvent evt);

    void Subscribe(Action<LifeCycleEvent> action);

    Task Start();

    Task Stop();
}
