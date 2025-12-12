namespace WorkflowCore.Models;

public interface IStepExecutionContext
{
    object Item { get; set; }

    ExecutionPointer ExecutionPointer { get; set; }

    object PersistenceData { get; set; }

    WorkflowStep Step { get; set; }

    WorkflowInstance Workflow { get; set; }

    CancellationToken CancellationToken { get; set; }
}