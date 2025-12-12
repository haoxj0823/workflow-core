namespace WorkflowCore.Models;

public interface IStepBody
{        
    Task<ExecutionResult> RunAsync(IStepExecutionContext context);        
}
