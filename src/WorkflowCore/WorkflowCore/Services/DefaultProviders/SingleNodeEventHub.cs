using Microsoft.Extensions.Logging;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Services.LifeCycleEvents;

namespace WorkflowCore.Services.DefaultProviders;

public class SingleNodeEventHub : ILifeCycleEventHub
{
    private readonly ICollection<Action<LifeCycleEvent>> _subscribers = [];
    private readonly ILogger<SingleNodeEventHub> _logger;

    public SingleNodeEventHub(ILogger<SingleNodeEventHub> logger)
    {
        _logger = logger;
    }

    public Task PublishNotification(LifeCycleEvent evt)
    {
        Task.Run(() =>
        {
            foreach (var subscriber in _subscribers)
            {
                try
                {
                    subscriber(evt);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(default, ex, $"Error on event subscriber: {ex.Message}");
                }
            }
        });
        return Task.CompletedTask;
    }

    public void Subscribe(Action<LifeCycleEvent> action)
    {
        _subscribers.Add(action);
    }

    public Task Start()
    {
        return Task.CompletedTask;
    }

    public Task Stop()
    {
        _subscribers.Clear();
        return Task.CompletedTask;
    }
}
