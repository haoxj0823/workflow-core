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

    public Task<string> StartWorkflowAsync(string workflowId, object data = null, string reference = null, CancellationToken cancellationToken = default)
    {
        return _workflowController.StartWorkflowAsync(workflowId, data, reference, cancellationToken);
    }

    public Task<string> StartWorkflowAsync(string workflowId, int? version, object data = null, string reference = null, CancellationToken cancellationToken = default)
    {
        return _workflowController.StartWorkflowAsync<object>(workflowId, version, data, reference, cancellationToken);
    }

    public Task<string> StartWorkflowAsync<TData>(string workflowId, TData data = null, string reference = null, CancellationToken cancellationToken = default)
        where TData : class, new()
    {
        return _workflowController.StartWorkflowAsync(workflowId, null, data, reference, cancellationToken);
    }

    public Task<string> StartWorkflowAsync<TData>(string workflowId, int? version, TData data = null, string reference = null, CancellationToken cancellationToken = default)
        where TData : class, new()
    {
        return _workflowController.StartWorkflowAsync(workflowId, version, data, reference, cancellationToken);
    }

    public Task PublishEventAsync(string eventName, string eventKey, object eventData, DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        return _workflowController.PublishEventAsync(eventName, eventKey, eventData, effectiveDate, cancellationToken);
    }

    public void Start()
    {
        StartAsync(CancellationToken.None).Wait();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var activity = WorkflowActivity.StartHost();
        try
        {
            _shutdown = false;

            PersistenceStore.EnsureStoreExists();

            await QueueProvider.StartAsync(cancellationToken);
            await _lifeCycleEventHub.StartAsync(cancellationToken);

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

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _shutdown = true;

        Logger.LogInformation("Stopping background tasks");
        Logger.LogInformation("Worker tasks stopped");

        await QueueProvider.StopAsync(cancellationToken);
        await _lifeCycleEventHub.StopAsync(cancellationToken);
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

    public Task<bool> SuspendWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        return _workflowController.SuspendWorkflowAsync(workflowId, cancellationToken);
    }

    public Task<bool> ResumeWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        return _workflowController.ResumeWorkflowAsync(workflowId, cancellationToken);
    }

    public Task<bool> TerminateWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        return _workflowController.TerminateWorkflowAsync(workflowId, cancellationToken);
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

    public Task<PendingActivity> GetPendingActivityAsync(string activityName, string workerId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return _activityController.GetPendingActivityAsync(activityName, workerId, timeout, cancellationToken);
    }

    public Task ReleaseActivityTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return _activityController.ReleaseActivityTokenAsync(token, cancellationToken);
    }

    public Task SubmitActivitySuccessAsync(string token, object result, CancellationToken cancellationToken = default)
    {
        return _activityController.SubmitActivitySuccessAsync(token, result, cancellationToken);
    }

    public Task SubmitActivityFailureAsync(string token, object result, CancellationToken cancellationToken = default)
    {
        return _activityController.SubmitActivityFailureAsync(token, result, cancellationToken);
    }

    private void AddEventSubscriptions()
    {
        _lifeCycleEventHub.Subscribe(HandleLifeCycleEvent);
    }
}
