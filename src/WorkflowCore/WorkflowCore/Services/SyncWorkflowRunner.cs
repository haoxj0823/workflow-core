using WorkflowCore.Exceptions;
using WorkflowCore.Models;
using WorkflowCore.Services.Executors;
using WorkflowCore.Services.Persistence;

namespace WorkflowCore.Services;

public class SyncWorkflowRunner : ISyncWorkflowRunner
{
    private readonly IWorkflowExecutor _executor;
    private readonly IDistributedLockProvider _lockService;
    private readonly IWorkflowRegistry _registry;
    private readonly IPersistenceProvider _persistenceStore;
    private readonly IExecutionPointerFactory _pointerFactory;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SyncWorkflowRunner(
        IWorkflowExecutor executor,
        IDistributedLockProvider lockService,
        IWorkflowRegistry registry,
        IPersistenceProvider persistenceStore,
        IExecutionPointerFactory pointerFactory,
        IDateTimeProvider dateTimeProvider)
    {
        _executor = executor;
        _lockService = lockService;
        _registry = registry;
        _persistenceStore = persistenceStore;
        _pointerFactory = pointerFactory;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<WorkflowInstance> RunWorkflowSync<TData>(string workflowId, int version, TData data, string reference, TimeSpan timeOut, bool persistSate = true, CancellationToken cancellationToken = default)
        where TData : new()
    {
        var timeoutCts = new CancellationTokenSource(timeOut);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

        return RunWorkflowSync(workflowId, version, data, reference, linkedCts.Token, persistSate);
    }

    public async Task<WorkflowInstance> RunWorkflowSync<TData>(string workflowId, int version, TData data, string reference, CancellationToken cancellationToken, bool persistSate = true)
        where TData : new()
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
            Status = WorkflowStatus.Suspended,
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

        var id = Guid.NewGuid().ToString();

        if (persistSate)
        {
            id = await _persistenceStore.CreateNewWorkflowAsync(wf, cancellationToken);
        }
        else
        {
            wf.Id = id;
        }

        wf.Status = WorkflowStatus.Runnable;

        if (!await _lockService.AcquireLockAsync(id, CancellationToken.None))
        {
            throw new InvalidOperationException();
        }

        try
        {
            while ((wf.Status == WorkflowStatus.Runnable) && !cancellationToken.IsCancellationRequested)
            {
                await _executor.ExecuteAsync(wf, cancellationToken);
                if (persistSate)
                {
                    await _persistenceStore.PersistWorkflowAsync(wf, cancellationToken);
                }
            }
        }
        finally
        {
            await _lockService.ReleaseLockAsync(id, CancellationToken.None);
        }

        return wf;
    }
}