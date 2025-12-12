using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Services;
using WorkflowCore.Services.DefaultProviders;
using WorkflowCore.Services.LifeCycleEvents;
using WorkflowCore.Services.Persistence;

namespace WorkflowCore.Models;

public class WorkflowOptions
{
    internal Func<IServiceProvider, IPersistenceProvider> PersistenceFactory;

    internal Func<IServiceProvider, IQueueProvider> QueueFactory;

    internal Func<IServiceProvider, IDistributedLockProvider> LockFactory;

    internal Func<IServiceProvider, ILifeCycleEventHub> EventHubFactory;

    public TimeSpan ErrorRetryInterval { get; set; } = TimeSpan.FromSeconds(60);

    public bool EnableLifeCycleEventsPublisher { get; set; } = true;

    public WorkflowOptions()
    {
        PersistenceFactory = new Func<IServiceProvider, IPersistenceProvider>(sp => new TransientMemoryPersistenceProvider(sp.GetService<ISingletonMemoryProvider>()));
        QueueFactory = new Func<IServiceProvider, IQueueProvider>(sp => new SingleNodeQueueProvider());
        LockFactory = new Func<IServiceProvider, IDistributedLockProvider>(sp => new SingleNodeLockProvider());
        EventHubFactory = new Func<IServiceProvider, ILifeCycleEventHub>(sp => new SingleNodeEventHub(sp.GetService<ILogger<SingleNodeEventHub>>()));
    }

    public void UsePersistence(Func<IServiceProvider, IPersistenceProvider> factory)
    {
        PersistenceFactory = factory;
    }

    public void UseDistributedLockManager(Func<IServiceProvider, IDistributedLockProvider> factory)
    {
        LockFactory = factory;
    }

    public void UseQueueProvider(Func<IServiceProvider, IQueueProvider> factory)
    {
        QueueFactory = factory;
    }

    public void UseEventHub(Func<IServiceProvider, ILifeCycleEventHub> factory)
    {
        EventHubFactory = factory;
    }
}
