namespace WorkflowCore.Services;

/// <summary>
/// Determines at which point to run the middleware.
/// </summary>
public enum WorkflowMiddlewarePhase
{
    /// <summary>
    /// The middleware should run before a workflow starts.
    /// </summary>
    PreWorkflow,

    /// <summary>
    /// The middleware should run after a workflow completes.
    /// </summary>
    PostWorkflow,

    /// <summary>
    /// The middleware should run after each workflow execution.
    /// </summary>
    ExecuteWorkflow
}
