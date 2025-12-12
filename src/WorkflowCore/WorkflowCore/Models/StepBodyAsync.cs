namespace WorkflowCore.Models;

public abstract class StepBodyAsync : IStepBody
{
    public abstract Task<ExecutionResult> RunAsync(IStepExecutionContext context);
}
