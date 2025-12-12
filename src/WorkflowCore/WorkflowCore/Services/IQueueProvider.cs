namespace WorkflowCore.Services;

/// <remarks>
/// The implementation of this interface will be responsible for
/// providing a (distributed) queueing mechanism to manage in flight workflows    
/// </remarks>
public interface IQueueProvider : IDisposable
{
    bool IsDequeueBlocking { get; }

    /// <summary>
    /// Enqueues work to be processed by a host in the cluster
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    Task QueueWorkAsync(string id, QueueType queue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the next work item from the front of the process queue.
    /// If the queue is empty, NULL is returned
    /// </summary>
    /// <returns></returns>
    Task<string> DequeueWorkAsync(QueueType queue, CancellationToken cancellationToken = default);

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
