using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Exceptions;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Services.LifeCycleEvents;
using WorkflowCore.Services.Middleware;
using WorkflowCore.Services.Persistence;

namespace WorkflowCore.Services;

public class WorkflowController : IWorkflowController
{
    private readonly IPersistenceProvider _persistenceStore;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IWorkflowRegistry _registry;
    private readonly IExecutionPointerFactory _pointerFactory;
    private readonly IQueueProvider _queueProvider;
    private readonly ILifeCycleEventHub _eventHub;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WorkflowController(
        IPersistenceProvider persistenceStore,
        IDistributedLockProvider lockProvider,
        IWorkflowRegistry registry,
        IExecutionPointerFactory pointerFactory,
        IQueueProvider queueProvider,
        ILifeCycleEventHub eventHub,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider,
        IDateTimeProvider dateTimeProvider)
    {
        _persistenceStore = persistenceStore;
        _lockProvider = lockProvider;
        _registry = registry;
        _pointerFactory = pointerFactory;
        _queueProvider = queueProvider;
        _eventHub = eventHub;
        _serviceProvider = serviceProvider;
        _logger = loggerFactory.CreateLogger<WorkflowController>();
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<string> StartWorkflowAsync(string workflowId, object data = null, string reference = null, CancellationToken cancellationToken = default)
    {
        return StartWorkflowAsync(workflowId, null, data, reference, cancellationToken);
    }

    public Task<string> StartWorkflowAsync(string workflowId, int? version, object data = null, string reference = null, CancellationToken cancellationToken = default)
    {
        return StartWorkflowAsync<object>(workflowId, version, data, reference, cancellationToken);
    }

    public Task<string> StartWorkflowAsync<TData>(string workflowId, TData data = null, string reference = null, CancellationToken cancellationToken = default)
        where TData : class, new()
    {
        return StartWorkflowAsync(workflowId, null, data, reference, cancellationToken);
    }

    public async Task<string> StartWorkflowAsync<TData>(string workflowId, int? version, TData data = null, string reference = null, CancellationToken cancellationToken = default)
        where TData : class, new()
    {
        var def = _registry.GetDefinition(workflowId, version);
        if (def == null)
        {
            throw new WorkflowNotRegisteredException(workflowId, version);
        }

        var wf = new WorkflowInstance
        {
            WorkflowDefinitionId = workflowId,
            Version = def.Version,
            Data = data,
            Description = def.Description,
            NextExecution = 0,
            CreateTime = _dateTimeProvider.UtcNow,
            Status = WorkflowStatus.Runnable,
            Reference = reference
        };

        if ((def.DataType != null) && (data == null))
        {
            if (typeof(TData) == def.DataType)
            {
                wf.Data = new TData();
            }
            else
            {
                wf.Data = def.DataType.GetConstructor([]).Invoke([]);
            }
        }

        wf.ExecutionPointers.Add(_pointerFactory.BuildGenesisPointer(def));

        using (var scope = _serviceProvider.CreateScope())
        {
            var middlewareRunner = scope.ServiceProvider.GetRequiredService<IWorkflowMiddlewareRunner>();
            await middlewareRunner.RunPreMiddlewareAsync(wf, def, cancellationToken);
        }

        string id = await _persistenceStore.CreateNewWorkflowAsync(wf, cancellationToken);

        await _queueProvider.QueueWorkAsync(id, QueueType.Workflow, cancellationToken);
        await _eventHub.PublishNotificationAsync(new WorkflowStarted
        {
            EventTimeUtc = _dateTimeProvider.UtcNow,
            Reference = reference,
            WorkflowInstanceId = id,
            WorkflowDefinitionId = def.Id,
            Version = def.Version
        }, cancellationToken);

        return id;
    }

    public async Task PublishEventAsync(string eventName, string eventKey, object eventData, DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating event {EventName} {EventKey}", eventName, eventKey);

        var evt = new Event();

        if (effectiveDate.HasValue)
        {
            evt.EventTime = effectiveDate.Value.ToUniversalTime();
        }
        else
        {
            evt.EventTime = _dateTimeProvider.UtcNow;
        }

        evt.EventData = eventData;
        evt.EventKey = eventKey;
        evt.EventName = eventName;
        evt.IsProcessed = false;

        var eventId = await _persistenceStore.CreateEventAsync(evt, cancellationToken);

        await _queueProvider.QueueWorkAsync(eventId, QueueType.Event, cancellationToken);
    }

    public async Task<bool> SuspendWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        if (!await _lockProvider.AcquireLockAsync(workflowId, CancellationToken.None))
        {
            return false;
        }

        try
        {
            var wf = await _persistenceStore.GetWorkflowInstanceAsync(workflowId, cancellationToken);
            if (wf.Status == WorkflowStatus.Runnable)
            {
                wf.Status = WorkflowStatus.Suspended;

                await _persistenceStore.PersistWorkflowAsync(wf, cancellationToken);
                await _eventHub.PublishNotificationAsync(new WorkflowSuspended
                {
                    EventTimeUtc = _dateTimeProvider.UtcNow,
                    Reference = wf.Reference,
                    WorkflowInstanceId = wf.Id,
                    WorkflowDefinitionId = wf.WorkflowDefinitionId,
                    Version = wf.Version
                }, cancellationToken);

                return true;
            }

            return false;
        }
        finally
        {
            await _lockProvider.ReleaseLockAsync(workflowId, CancellationToken.None);
        }
    }

    public async Task<bool> ResumeWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        if (!await _lockProvider.AcquireLockAsync(workflowId, CancellationToken.None))
        {
            return false;
        }

        bool requeue = false;

        try
        {
            var wf = await _persistenceStore.GetWorkflowInstanceAsync(workflowId, cancellationToken);
            if (wf.Status == WorkflowStatus.Suspended)
            {
                wf.Status = WorkflowStatus.Runnable;

                await _persistenceStore.PersistWorkflowAsync(wf, cancellationToken);

                requeue = true;

                await _eventHub.PublishNotificationAsync(new WorkflowResumed
                {
                    EventTimeUtc = _dateTimeProvider.UtcNow,
                    Reference = wf.Reference,
                    WorkflowInstanceId = wf.Id,
                    WorkflowDefinitionId = wf.WorkflowDefinitionId,
                    Version = wf.Version
                }, cancellationToken);

                return true;
            }

            return false;
        }
        finally
        {
            await _lockProvider.ReleaseLockAsync(workflowId, CancellationToken.None);

            if (requeue)
            {
                await _queueProvider.QueueWorkAsync(workflowId, QueueType.Workflow, cancellationToken);
            }
        }
    }

    public async Task<bool> TerminateWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        if (!await _lockProvider.AcquireLockAsync(workflowId, CancellationToken.None))
        {
            return false;
        }

        try
        {
            var wf = await _persistenceStore.GetWorkflowInstanceAsync(workflowId, cancellationToken);

            wf.Status = WorkflowStatus.Terminated;
            wf.CompleteTime = _dateTimeProvider.UtcNow;

            await _persistenceStore.PersistWorkflowAsync(wf, cancellationToken);
            await _eventHub.PublishNotificationAsync(new WorkflowTerminated
            {
                EventTimeUtc = _dateTimeProvider.UtcNow,
                Reference = wf.Reference,
                WorkflowInstanceId = wf.Id,
                WorkflowDefinitionId = wf.WorkflowDefinitionId,
                Version = wf.Version
            }, cancellationToken);

            return true;
        }
        finally
        {
            await _lockProvider.ReleaseLockAsync(workflowId, CancellationToken.None);
        }
    }

    public void RegisterWorkflow<TWorkflow>()
        where TWorkflow : IWorkflow
    {
        var wf = ActivatorUtilities.CreateInstance<TWorkflow>(_serviceProvider);
        _registry.RegisterWorkflow(wf);
    }

    public void RegisterWorkflow<TWorkflow, TData>()
        where TWorkflow : IWorkflow<TData>
        where TData : new()
    {
        var wf = ActivatorUtilities.CreateInstance<TWorkflow>(_serviceProvider);
        _registry.RegisterWorkflow(wf);
    }
}