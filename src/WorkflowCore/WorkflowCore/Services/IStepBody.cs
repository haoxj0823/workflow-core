using WorkflowCore.Models;

namespace WorkflowCore.Services;

public interface IStepBody
{        
    Task<ExecutionResult> RunAsync(IStepExecutionContext context);        
}
