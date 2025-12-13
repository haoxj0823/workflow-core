using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services.LifeCycleEvents;

public class LifeCycleEventPublisher : ILifeCycleEventPublisher, IDisposable
{
    private readonly ILifeCycleEventHub _eventHub;
    private readonly WorkflowOptions _workflowOptions;
    private readonly ILogger<LifeCycleEventPublisher> _logger;
    private BlockingCollection<LifeCycleEvent> _outbox;
    private Task _dispatchTask;

    public LifeCycleEventPublisher(
        ILifeCycleEventHub eventHub,
        IOptions<WorkflowOptions> workflowOptions,
        ILogger<LifeCycleEventPublisher> logger)
    {
        _eventHub = eventHub;
        _workflowOptions = workflowOptions.Value;
        _logger = logger;
        _outbox = [];
    }

    public void PublishNotification(LifeCycleEvent evt)
    {
        if (_outbox.IsAddingCompleted || !_workflowOptions.EnableLifeCycleEventsPublisher)
        {
            return;
        }

        _outbox.Add(evt);
    }

    public void Start()
    {
        if (_dispatchTask != null)
        {
            throw new InvalidOperationException();
        }

        if (_outbox.IsAddingCompleted)
        {
            _outbox = [];
        }

        _dispatchTask = new Task(async () => await ExecuteAsync());
        _dispatchTask.Start();
    }

    public void Stop()
    {
        _outbox.CompleteAdding();

        _dispatchTask.Wait();
        _dispatchTask = null;
    }

    public void Dispose()
    {
        _outbox.Dispose();

        GC.SuppressFinalize(this);
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var evt in _outbox.GetConsumingEnumerable(cancellationToken))
        {
            try
            {
                await _eventHub.PublishNotificationAsync(evt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(default, ex, "{Message}", ex.Message);
            }
        }
    }
}