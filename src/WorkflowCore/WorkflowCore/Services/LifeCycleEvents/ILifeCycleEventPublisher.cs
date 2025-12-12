using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services.LifeCycleEvents;

public interface ILifeCycleEventPublisher
{
    void PublishNotification(LifeCycleEvent evt);
}
