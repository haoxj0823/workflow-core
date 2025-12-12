using WorkflowCore.Models;

namespace WorkflowCore.Services.Processors;

public interface ICancellationProcessor
{
    void ProcessCancellations(WorkflowInstance workflow, WorkflowDefinition workflowDef, WorkflowExecutorResult executionResult);
}
