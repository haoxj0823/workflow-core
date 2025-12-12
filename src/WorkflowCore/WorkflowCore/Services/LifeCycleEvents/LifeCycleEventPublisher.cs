using Microsoft.Extensions.Logging;
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
        WorkflowOptions workflowOptions,
        ILogger<LifeCycleEventPublisher> logger)
    {
        _eventHub = eventHub;
        _workflowOptions = workflowOptions;
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

        _dispatchTask = new Task(Execute);
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
    }

    private async void Execute()
    {
        foreach (var evt in _outbox.GetConsumingEnumerable())
        {
            try
            {
                await _eventHub.PublishNotification(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(default, ex, ex.Message);
            }
        }
    }
}