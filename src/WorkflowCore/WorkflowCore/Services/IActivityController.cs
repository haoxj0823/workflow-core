namespace WorkflowCore.Services;

public interface IActivityController
{
    Task<PendingActivity> GetPendingActivityAsync(string activityName, string workerId, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    Task ReleaseActivityTokenAsync(string token, CancellationToken cancellationToken = default);

    Task SubmitActivitySuccessAsync(string token, object result, CancellationToken cancellationToken = default);

    Task SubmitActivityFailureAsync(string token, object result, CancellationToken cancellationToken = default);
}
