using System.Linq.Expressions;

namespace WorkflowCore.Models;

public interface ISubscriptionStep 
{
    string EventName { get; set; }

    LambdaExpression EventKey { get; set; }
}

public interface ISubscriptionStep<TStepBody> : ISubscriptionStep
    where TStepBody : ISubscriptionBody
{
}
