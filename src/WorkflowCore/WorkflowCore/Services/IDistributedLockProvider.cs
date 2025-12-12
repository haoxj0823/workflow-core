namespace WorkflowCore.Services;

/// <remarks>
/// The implementation of this interface will be responsible for
/// providing a (distributed) locking mechanism to manage in flight workflows    
/// </remarks>
public interface IDistributedLockProvider
{
    /// <summary>
    /// Acquire a lock on the specified resource.
    /// </summary>
    /// <param name="Id">Resource ID to lock.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>`true`, if the lock was acquired.</returns>
    Task<bool> AcquireLockAsync(string Id, CancellationToken cancellationToken = default);

    Task ReleaseLockAsync(string Id, CancellationToken cancellationToken = default);
}
