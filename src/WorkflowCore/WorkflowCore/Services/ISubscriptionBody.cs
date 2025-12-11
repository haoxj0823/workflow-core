namespace WorkflowCore.Services;

public interface ISubscriptionBody : IStepBody
{
    object EventData { get; set; }        
}
