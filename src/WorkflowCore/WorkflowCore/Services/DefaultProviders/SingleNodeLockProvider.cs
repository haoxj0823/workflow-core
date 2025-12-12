namespace WorkflowCore.Services.DefaultProviders;

public class SingleNodeLockProvider : IDistributedLockProvider
{
    private readonly HashSet<string> _locks = [];

    public Task<bool> AcquireLock(string Id, CancellationToken cancellationToken)
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

    public Task ReleaseLock(string Id)
    {
        lock (_locks)
        {
            _locks.Remove(Id);
            return Task.CompletedTask;
        }
    }
}