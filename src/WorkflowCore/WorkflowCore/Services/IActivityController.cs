namespace WorkflowCore.Services;

public interface IActivityController
{
    Task<PendingActivity> GetPendingActivity(string activityName, string workerId, TimeSpan? timeout = null);

    Task ReleaseActivityToken(string token);

    Task SubmitActivitySuccess(string token, object result);

    Task SubmitActivityFailure(string token, object result);
}
