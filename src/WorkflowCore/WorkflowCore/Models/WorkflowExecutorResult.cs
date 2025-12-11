namespace WorkflowCore.Models;

public class WorkflowExecutorResult
{
    public List<EventSubscription> Subscriptions { get; set; } = [];

    public List<ExecutionError> Errors { get; set; } = [];
}
