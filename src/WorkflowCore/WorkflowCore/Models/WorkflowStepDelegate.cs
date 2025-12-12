namespace WorkflowCore.Models;

public delegate Task<ExecutionResult> WorkflowStepDelegate(CancellationToken cancellationToken = default);
