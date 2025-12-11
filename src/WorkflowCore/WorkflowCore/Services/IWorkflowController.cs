namespace WorkflowCore.Services;

public interface IWorkflowController
{
    Task<string> StartWorkflow(string workflowId, object data = null, string reference=null);

    Task<string> StartWorkflow(string workflowId, int? version, object data = null, string reference=null);

    Task<string> StartWorkflow<TData>(string workflowId, TData data = null, string reference=null) where TData : class, new();

    Task<string> StartWorkflow<TData>(string workflowId, int? version, TData data = null, string reference=null) where TData : class, new();

    Task PublishEvent(string eventName, string eventKey, object eventData, DateTime? effectiveDate = null);

    void RegisterWorkflow<TWorkflow>() where TWorkflow : IWorkflow;

    void RegisterWorkflow<TWorkflow, TData>() where TWorkflow : IWorkflow<TData> where TData : new();

    Task<bool> SuspendWorkflow(string workflowId);

    Task<bool> ResumeWorkflow(string workflowId);

    Task<bool> TerminateWorkflow(string workflowId);
}
