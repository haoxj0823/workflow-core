using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services;

public interface ILifeCycleEventPublisher
{
    void PublishNotification(LifeCycleEvent evt);
}
