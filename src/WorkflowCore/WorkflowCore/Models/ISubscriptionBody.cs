namespace WorkflowCore.Models;

public interface ISubscriptionBody : IStepBody
{
    object EventData { get; set; }        
}
