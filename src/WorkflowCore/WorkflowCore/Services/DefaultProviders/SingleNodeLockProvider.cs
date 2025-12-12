namespace WorkflowCore.Services.DefaultProviders;

public class SingleNodeLockProvider : IDistributedLockProvider
{
    private readonly HashSet<string> _locks = [];

    public Task<bool> AcquireLockAsync(string Id, CancellationToken cancellationToken = default)
    {
        lock (_locks)
        {
            if (_locks.Contains(Id))
            {
                return Task.FromResult(false);
            }

            _locks.Add(Id);

            return Task.FromResult(true);
        }
    }

    public Task ReleaseLockAsync(string Id, CancellationToken cancellationToken = default)
    {
        lock (_locks)
        {
            _locks.Remove(Id);
            return Task.CompletedTask;
        }
    }
}