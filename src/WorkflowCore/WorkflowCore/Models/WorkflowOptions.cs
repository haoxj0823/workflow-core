using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Services;
using WorkflowCore.Services.LifeCycleEvents;
using WorkflowCore.Services.Persistence;

namespace WorkflowCore.Models;

public class WorkflowOptions
{
    internal Func<IServiceProvider, IPersistenceProvider> PersistenceFactory;

    internal Func<IServiceProvider, IDistributedLockProvider> LockFactory;

    internal Func<IServiceProvider, ILifeCycleEventHub> EventHubFactory;

    internal TimeSpan PollInterval;

    internal TimeSpan IdleTime;

    internal TimeSpan ErrorRetryInterval;

    internal int MaxConcurrentWorkflows = Math.Max(Environment.ProcessorCount, 4);

    public IServiceCollection Services { get; private set; }

    public WorkflowOptions(IServiceCollection services)
    {
        Services = services;
        PollInterval = TimeSpan.FromSeconds(10);
        IdleTime = TimeSpan.FromMilliseconds(100);
        ErrorRetryInterval = TimeSpan.FromSeconds(60);

        LockFactory = new Func<IServiceProvider, IDistributedLockProvider>(sp => new SingleNodeLockProvider());
        PersistenceFactory = new Func<IServiceProvider, IPersistenceProvider>(sp => new TransientMemoryPersistenceProvider(sp.GetService<ISingletonMemoryProvider>()));
        EventHubFactory = new Func<IServiceProvider, ILifeCycleEventHub>(sp => new SingleNodeEventHub(sp.GetService<ILogger<SingleNodeEventHub>>()));
    }

    public bool EnableWorkflows { get; set; } = true;

    public bool EnableEvents { get; set; } = true;

    public bool EnableIndexes { get; set; } = true;

    public bool EnablePolling { get; set; } = true;

    public bool EnableLifeCycleEventsPublisher { get; set; } = true;

    public void UsePersistence(Func<IServiceProvider, IPersistenceProvider> factory)
    {
        PersistenceFactory = factory;
    }

    public void UseDistributedLockManager(Func<IServiceProvider, IDistributedLockProvider> factory)
    {
        LockFactory = factory;
    }

    public void UseEventHub(Func<IServiceProvider, ILifeCycleEventHub> factory)
    {
        EventHubFactory = factory;
    }

    public void UsePollInterval(TimeSpan interval)
    {
        PollInterval = interval;
    }

    public void UseErrorRetryInterval(TimeSpan interval)
    {
        ErrorRetryInterval = interval;
    }

    public void UseIdleTime(TimeSpan interval)
    {
        IdleTime = interval;
    }

    public void UseMaxConcurrentWorkflows(int maxConcurrentWorkflows)
    {
        MaxConcurrentWorkflows = maxConcurrentWorkflows;
    }
}
    
