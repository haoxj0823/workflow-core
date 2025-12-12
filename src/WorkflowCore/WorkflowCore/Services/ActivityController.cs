using Newtonsoft.Json;
using System.Text;
using WorkflowCore.Exceptions;
using WorkflowCore.Models;
using WorkflowCore.Services.Persistence;

namespace WorkflowCore.Services;

public class ActivityController : IActivityController
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkflowController _workflowController;

    public ActivityController(
        ISubscriptionRepository subscriptionRepository,
        IWorkflowController workflowController,
        IDateTimeProvider dateTimeProvider,
        IDistributedLockProvider lockProvider)
    {
        _subscriptionRepository = subscriptionRepository;
        _dateTimeProvider = dateTimeProvider;
        _lockProvider = lockProvider;
        _workflowController = workflowController;
    }

    public async Task<PendingActivity> GetPendingActivityAsync(string activityName, string workerId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var endTime = _dateTimeProvider.UtcNow.Add(timeout ?? TimeSpan.Zero);
        var firstPass = true;
        EventSubscription subscription = null;
        while ((subscription == null && _dateTimeProvider.UtcNow < endTime) || firstPass)
        {
            if (!firstPass)
            {
                await Task.Delay(100, cancellationToken);
            }

            subscription = await _subscriptionRepository.GetFirstOpenSubscriptionAsync(Event.EventTypeActivity, activityName, _dateTimeProvider.UtcNow, cancellationToken);
            if (subscription != null)
            {
                if (!await _lockProvider.AcquireLockAsync($"sub:{subscription.Id}", CancellationToken.None))
                {
                    subscription = null;
                }
            }

            firstPass = false;
        }

        if (subscription == null)
        {
            return null;
        }

        try
        {
            var token = Token.Create(subscription.Id, subscription.EventKey);
            var result = new PendingActivity
            {
                Token = token.Encode(),
                ActivityName = subscription.EventKey,
                Parameters = subscription.SubscriptionData,
                TokenExpiry = new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Utc)
            };

            if (!await _subscriptionRepository.SetSubscriptionTokenAsync(subscription.Id, result.Token, workerId, result.TokenExpiry, cancellationToken))
            {
                return null;
            }

            return result;
        }
        finally
        {
            await _lockProvider.ReleaseLockAsync($"sub:{subscription.Id}", CancellationToken.None);
        }
    }

    public async Task ReleaseActivityTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenObj = Token.Decode(token);
        await _subscriptionRepository.ClearSubscriptionTokenAsync(tokenObj.SubscriptionId, token, cancellationToken);
    }

    public async Task SubmitActivitySuccessAsync(string token, object result, CancellationToken cancellationToken = default)
    {
        await SubmitActivityResult(token, new ActivityResult
        {
            Data = result,
            Status = ActivityResult.StatusType.Success
        }, cancellationToken);
    }

    public async Task SubmitActivityFailureAsync(string token, object result, CancellationToken cancellationToken = default)
    {
        await SubmitActivityResult(token, new ActivityResult
        {
            Data = result,
            Status = ActivityResult.StatusType.Fail
        }, cancellationToken);
    }

    private async Task SubmitActivityResult(string token, ActivityResult result, CancellationToken cancellationToken = default)
    {
        var tokenObj = Token.Decode(token);
        var sub = await _subscriptionRepository.GetSubscriptionAsync(tokenObj.SubscriptionId, cancellationToken);
        if (sub == null)
        {
            throw new NotFoundException();
        }

        if (sub.ExternalToken != token)
        {
            throw new NotFoundException("Token mismatch");
        }

        result.SubscriptionId = sub.Id;

        await _workflowController.PublishEventAsync(sub.EventName, sub.EventKey, result, null, cancellationToken);
    }

    class Token
    {
        public string SubscriptionId { get; set; }

        public string ActivityName { get; set; }

        public string Nonce { get; set; }

        public string Encode()
        {
            var json = JsonConvert.SerializeObject(this);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static Token Create(string subscriptionId, string activityName)
        {
            return new Token
            {
                SubscriptionId = subscriptionId,
                ActivityName = activityName,
                Nonce = Guid.NewGuid().ToString()
            };
        }

        public static Token Decode(string encodedToken)
        {
            var raw = Convert.FromBase64String(encodedToken);
            var json = Encoding.UTF8.GetString(raw);
            return JsonConvert.DeserializeObject<Token>(json);
        }
    }
}