namespace WorkflowCore.Models;

public class WorkflowOptions
{
    public TimeSpan ErrorRetryInterval { get; set; } = TimeSpan.FromSeconds(60);

    public bool EnableLifeCycleEventsPublisher { get; set; } = true;
}
