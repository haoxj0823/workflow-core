using System.Collections.Concurrent;

namespace WorkflowCore.Services.DefaultProviders;

public class SingleNodeQueueProvider : IQueueProvider
{
    private readonly Dictionary<QueueType, BlockingCollection<string>> _queues = new()
    {
        [QueueType.Workflow] = [],
        [QueueType.Event] = [],
    };

    public bool IsDequeueBlocking => true;

    public Task QueueWorkAsync(string id, QueueType queue, CancellationToken cancellationToken)
    {
        _queues[queue].Add(id, cancellationToken);
        return Task.CompletedTask;
    }

    public Task<string> DequeueWorkAsync(QueueType queue, CancellationToken cancellationToken)
    {
        if (_queues[queue].TryTake(out string id, 100, cancellationToken))
        {
            return Task.FromResult(id);
        }
        return Task.FromResult<string>(null);
    }

    public Task StartAsync()
    {
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
