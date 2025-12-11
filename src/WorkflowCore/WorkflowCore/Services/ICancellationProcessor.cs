using WorkflowCore.Models;

namespace WorkflowCore.Services;

public interface ICancellationProcessor
{
    void ProcessCancellations(WorkflowInstance workflow, WorkflowDefinition workflowDef, WorkflowExecutorResult executionResult);
}
