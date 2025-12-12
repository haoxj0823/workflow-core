using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Services.LifeCycleEvents;
using WorkflowCore.Services.Persistence;

namespace WorkflowCore.Services.DefaultProviders;

public class DefaultWorkflowHost : IWorkflowHost, IDisposable
{
    private bool _shutdown = true;

    private readonly IWorkflowController _workflowController;
    private readonly IActivityController _activityController;
    private readonly ILifeCycleEventHub _lifeCycleEventHub;

    public event StepErrorEventHandler OnStepError;
    public event LifeCycleEventHandler OnLifeCycleEvent;

    public IPersistenceProvider PersistenceStore { get; private set; }

    public IDistributedLockProvider LockProvider { get; private set; }

    public IQueueProvider QueueProvider { get; private set; }

    public IWorkflowRegistry Registry { get; private set; }

    public WorkflowOptions WorkflowOptions { get; private set; }

    public ILogger Logger { get; private set; }

    public DefaultWorkflowHost(
        IPersistenceProvider persistenceStore,
        IQueueProvider queueProvider,
        IDistributedLockProvider lockProvider,
        IWorkflowRegistry registry,
        IOptions<WorkflowOptions> workflowOptions,
        ILogger<DefaultWorkflowHost> logger,
        IWorkflowController workflowController,
        ILifeCycleEventHub lifeCycleEventHub,
        IActivityController activityController)
    {
        PersistenceStore = persistenceStore;
        QueueProvider = queueProvider;
        LockProvider = lockProvider;
        Registry = registry;
        WorkflowOptions = workflowOptions.Value;
        Logger = logger;

        _workflowController = workflowController;
        _activityController = activityController;
        _lifeCycleEventHub = lifeCycleEventHub;
    }

    public Task<string> StartWorkflow(string workflowId, object data = null, string reference = null)
    {
        return _workflowController.StartWorkflow(workflowId, data, reference);
    }

    public Task<string> StartWorkflow(string workflowId, int? version, object data = null, string reference = null)
    {
        return _workflowController.StartWorkflow<object>(workflowId, version, data, reference);
    }

    public Task<string> StartWorkflow<TData>(string workflowId, TData data = null, string reference = null)
        where TData : class, new()
    {
        return _workflowController.StartWorkflow(workflowId, null, data, reference);
    }

    public Task<string> StartWorkflow<TData>(string workflowId, int? version, TData data = null, string reference = null)
        where TData : class, new()
    {
        return _workflowController.StartWorkflow(workflowId, version, data, reference);
    }

    public Task PublishEvent(string eventName, string eventKey, object eventData, DateTime? effectiveDate = null)
    {
        return _workflowController.PublishEvent(eventName, eventKey, eventData, effectiveDate);
    }

    public void Start()
    {
        StartAsync(CancellationToken.None).Wait();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var activity = WorkflowActivity.StartHost();
        try
        {
            _shutdown = false;

            PersistenceStore.EnsureStoreExists();

            await QueueProvider.StartAsync();
            await _lifeCycleEventHub.Start();

            AddEventSubscriptions();

            Logger.LogInformation("Starting background tasks");
        }
        catch (Exception ex)
        {
            activity.AddException(ex);
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public void Stop()
    {
        StopAsync(CancellationToken.None).Wait();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _shutdown = true;

        Logger.LogInformation("Stopping background tasks");
        Logger.LogInformation("Worker tasks stopped");

        await QueueProvider.StopAsync();
        await _lifeCycleEventHub.Stop();
    }

    public void RegisterWorkflow<TWorkflow>()
        where TWorkflow : IWorkflow
    {
        _workflowController.RegisterWorkflow<TWorkflow>();
    }

    public void RegisterWorkflow<TWorkflow, TData>()
        where TWorkflow : IWorkflow<TData>
        where TData : new()
    {
        _workflowController.RegisterWorkflow<TWorkflow, TData>();
    }

    public Task<bool> SuspendWorkflow(string workflowId)
    {
        return _workflowController.SuspendWorkflow(workflowId);
    }

    public Task<bool> ResumeWorkflow(string workflowId)
    {
        return _workflowController.ResumeWorkflow(workflowId);
    }

    public Task<bool> TerminateWorkflow(string workflowId)
    {
        return _workflowController.TerminateWorkflow(workflowId);
    }

    public void HandleLifeCycleEvent(LifeCycleEvent evt)
    {
        OnLifeCycleEvent?.Invoke(evt);
    }

    public void ReportStepError(WorkflowInstance workflow, WorkflowStep step, Exception exception)
    {
        OnStepError?.Invoke(workflow, step, exception);
    }

    public void Dispose()
    {
        if (!_shutdown)
        {
            Stop();
        }

        GC.SuppressFinalize(this);
    }

    public Task<PendingActivity> GetPendingActivity(string activityName, string workerId, TimeSpan? timeout = null)
    {
        return _activityController.GetPendingActivity(activityName, workerId, timeout);
    }

    public Task ReleaseActivityToken(string token)
    {
        return _activityController.ReleaseActivityToken(token);
    }

    public Task SubmitActivitySuccess(string token, object result)
    {
        return _activityController.SubmitActivitySuccess(token, result);
    }

    public Task SubmitActivityFailure(string token, object result)
    {
        return _activityController.SubmitActivityFailure(token, result);
    }

    private void AddEventSubscriptions()
    {
        _lifeCycleEventHub.Subscribe(HandleLifeCycleEvent);
    }
}
