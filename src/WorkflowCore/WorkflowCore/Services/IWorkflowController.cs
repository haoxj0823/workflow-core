namespace WorkflowCore.Services;

public interface IWorkflowController
{
    Task<string> StartWorkflowAsync(string workflowId, object data = null, string reference = null, CancellationToken cancellationToken = default);

    Task<string> StartWorkflowAsync(string workflowId, int? version, object data = null, string reference = null, CancellationToken cancellationToken = default);

    Task<string> StartWorkflowAsync<TData>(string workflowId, TData data = null, string reference = null, CancellationToken cancellationToken = default) where TData
        : class, new();

    Task<string> StartWorkflowAsync<TData>(string workflowId, int? version, TData data = null, string reference = null, CancellationToken cancellationToken = default)
        where TData : class, new();

    Task PublishEventAsync(string eventName, string eventKey, object eventData, DateTime? effectiveDate = null, CancellationToken cancellationToken = default);

    Task<bool> SuspendWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);

    Task<bool> ResumeWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);

    Task<bool> TerminateWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);

    void RegisterWorkflow<TWorkflow>() where TWorkflow : IWorkflow;

    void RegisterWorkflow<TWorkflow, TData>() where TWorkflow : IWorkflow<TData>
        where TData : new();
}
