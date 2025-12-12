using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Services.LifeCycleEvents;

namespace WorkflowCore.Services.Processors;

public class ExecutionResultProcessor : IExecutionResultProcessor
{
    private readonly IExecutionPointerFactory _pointerFactory;
    private readonly IDateTimeProvider _datetimeProvider;
    private readonly ILifeCycleEventPublisher _eventPublisher;
    private readonly IEnumerable<IWorkflowErrorHandler> _errorHandlers;

    public ExecutionResultProcessor(
        IExecutionPointerFactory pointerFactory,
        IDateTimeProvider datetimeProvider,
        ILifeCycleEventPublisher eventPublisher,
        IEnumerable<IWorkflowErrorHandler> errorHandlers)
    {
        _pointerFactory = pointerFactory;
        _datetimeProvider = datetimeProvider;
        _eventPublisher = eventPublisher;
        _errorHandlers = errorHandlers;
    }

    public void ProcessExecutionResult(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, ExecutionResult result, WorkflowExecutorResult workflowResult)
    {
        pointer.PersistenceData = result.PersistenceData;
        pointer.Outcome = result.OutcomeValue;

        if (result.SleepFor.HasValue)
        {
            pointer.SleepUntil = _datetimeProvider.UtcNow.Add(result.SleepFor.Value);
            pointer.Status = PointerStatus.Sleeping;
        }

        if (!string.IsNullOrEmpty(result.EventName))
        {
            pointer.EventName = result.EventName;
            pointer.EventKey = result.EventKey;
            pointer.Active = false;
            pointer.Status = PointerStatus.WaitingForEvent;

            workflowResult.Subscriptions.Add(new EventSubscription
            {
                WorkflowId = workflow.Id,
                StepId = pointer.StepId,
                ExecutionPointerId = pointer.Id,
                EventName = pointer.EventName,
                EventKey = pointer.EventKey,
                SubscribeAsOf = result.EventAsOf,
                SubscriptionData = result.SubscriptionData
            });
        }

        if (result.Proceed)
        {
            pointer.Active = false;
            pointer.EndTime = _datetimeProvider.UtcNow;
            pointer.Status = PointerStatus.Complete;

            var outcomes = step.Outcomes ?? [];
            foreach (var outcomeTarget in outcomes.Where(x => x.Matches(result, workflow.Data)))
            {
                workflow.ExecutionPointers.Add(_pointerFactory.BuildNextPointer(def, pointer, outcomeTarget));
            }

            var pendingSubsequentPointers = workflow.ExecutionPointers
                .FindByStatus(PointerStatus.PendingPredecessor)
                .Where(x => x.PredecessorId == pointer.Id);

            foreach (var subsequent in pendingSubsequentPointers)
            {
                subsequent.Status = PointerStatus.Pending;
                subsequent.Active = true;
            }

            _eventPublisher.PublishNotification(new StepCompleted
            {
                EventTimeUtc = _datetimeProvider.UtcNow,
                Reference = workflow.Reference,
                ExecutionPointerId = pointer.Id,
                StepId = step.Id,
                WorkflowInstanceId = workflow.Id,
                WorkflowDefinitionId = workflow.WorkflowDefinitionId,
                Version = workflow.Version
            });
        }
        else
        {
            var branchValues = result.BranchValues ?? [];
            var children = step.Children ?? [];

            foreach (var branch in branchValues)
            {
                foreach (var childDefId in children)
                {
                    workflow.ExecutionPointers.Add(_pointerFactory.BuildChildPointer(def, pointer, childDefId, branch));
                }
            }
        }
    }

    public void HandleStepException(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, Exception exception)
    {
        _eventPublisher.PublishNotification(new WorkflowError
        {
            EventTimeUtc = _datetimeProvider.UtcNow,
            Reference = workflow.Reference,
            WorkflowInstanceId = workflow.Id,
            WorkflowDefinitionId = workflow.WorkflowDefinitionId,
            Version = workflow.Version,
            ExecutionPointerId = pointer.Id,
            StepId = step.Id,
            Message = exception.Message
        });

        pointer.Status = PointerStatus.Failed;

        var queue = new Queue<ExecutionPointer>();
        queue.Enqueue(pointer);

        while (queue.Count > 0)
        {
            var exceptionPointer = queue.Dequeue();
            var exceptionStep = def.Steps.FindById(exceptionPointer.StepId);
            var shouldCompensate = ShouldCompensate(workflow, def, exceptionPointer);
            var errorOption = exceptionStep.ErrorBehavior ?? (shouldCompensate ? WorkflowErrorHandling.Compensate : def.DefaultErrorBehavior);

            foreach (var handler in _errorHandlers.Where(x => x.Type == errorOption))
            {
                handler.Handle(workflow, def, exceptionPointer, exceptionStep, exception, queue);
            }
        }
    }

    private static bool ShouldCompensate(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer currentPointer)
    {
        var scope = new Stack<string>(currentPointer.Scope ?? []);
        scope.Push(currentPointer.Id);

        while (scope.Count > 0)
        {
            var pointerId = scope.Pop();
            var pointer = workflow.ExecutionPointers.FindById(pointerId);
            var step = def.Steps.FindById(pointer.StepId);
            if (step.CompensationStepId.HasValue || step.RevertChildrenAfterCompensation)
            {
                return true;
            }
        }

        return false;
    }
}