using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkflowCore.Models;
using WorkflowCore.Services.LifeCycleEvents;
using WorkflowCore.Services.Persistence;

namespace WorkflowCore.Services;

public interface IWorkflowHost : IWorkflowController, IActivityController, IHostedService
{
    event StepErrorEventHandler OnStepError;

    event LifeCycleEventHandler OnLifeCycleEvent;

    IPersistenceProvider PersistenceStore { get; }

    IDistributedLockProvider LockProvider { get; }

    IWorkflowRegistry Registry { get; }

    IQueueProvider QueueProvider { get; }

    WorkflowOptions WorkflowOptions { get; }

    ILogger Logger { get; }

    void ReportStepError(WorkflowInstance workflow, WorkflowStep step, Exception exception);

    void Start();

    void Stop();
}
