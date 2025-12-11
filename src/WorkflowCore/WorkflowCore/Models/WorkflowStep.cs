using System.Linq.Expressions;
using WorkflowCore.Services;

namespace WorkflowCore.Models;

public abstract class WorkflowStep
{
    public abstract Type BodyType { get; }

    public virtual int Id { get; set; }

    public virtual string Name { get; set; }

    public virtual string ExternalId { get; set; }

    public virtual List<int> Children { get; set; } = [];

    public virtual List<IStepOutcome> Outcomes { get; set; } = [];

    public virtual List<IStepParameter> Inputs { get; set; } = [];

    public virtual List<IStepParameter> Outputs { get; set; } = [];

    public virtual WorkflowErrorHandling? ErrorBehavior { get; set; }

    public virtual TimeSpan? RetryInterval { get; set; }

    public virtual int? CompensationStepId { get; set; }

    public virtual bool ResumeChildrenAfterCompensation => true;

    public virtual bool RevertChildrenAfterCompensation => false;

    public virtual LambdaExpression CancelCondition { get; set; }

    public bool ProceedOnCancel { get; set; } = false;

    public virtual ExecutionPipelineDirective InitForExecution(WorkflowExecutorResult executorResult, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
    {
        return ExecutionPipelineDirective.Next;
    }

    public virtual ExecutionPipelineDirective BeforeExecute(WorkflowExecutorResult executorResult, IStepExecutionContext context, ExecutionPointer executionPointer, IStepBody body)
    {
        return ExecutionPipelineDirective.Next;
    }

    public virtual void AfterExecute(WorkflowExecutorResult executorResult, IStepExecutionContext context, ExecutionResult stepResult, ExecutionPointer executionPointer)
    {
    }

    public virtual void PrimeForRetry(ExecutionPointer pointer)
    {
    }

    public virtual void AfterWorkflowIteration(WorkflowExecutorResult executorResult, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
    {
    }

    public virtual IStepBody ConstructBody(IServiceProvider serviceProvider)
    {
        IStepBody body = (serviceProvider.GetService(BodyType) as IStepBody);
        if (body == null)
        {
            var stepCtor = BodyType.GetConstructor([]);
            if (stepCtor != null)
            {
                body = (stepCtor.Invoke(null) as IStepBody);
            }
        }
        return body;
    }
}

public class WorkflowStep<TStepBody> : WorkflowStep
    where TStepBody : IStepBody
{
    public override Type BodyType => typeof(TStepBody);
}
