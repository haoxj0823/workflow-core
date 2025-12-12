using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Services.LifeCycleEvents;
using WorkflowCore.Services.Middleware;
using WorkflowCore.Services.Processors;

namespace WorkflowCore.Services.Executors;

public class WorkflowExecutor : IWorkflowExecutor
{
    private readonly IWorkflowRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly IScopeProvider _scopeProvider;
    private readonly IDateTimeProvider _datetimeProvider;
    private readonly IExecutionResultProcessor _executionResultProcessor;
    private readonly ICancellationProcessor _cancellationProcessor;
    private readonly IExecutionScheduler _executionScheduler;
    private readonly ILifeCycleEventPublisher _publisher;
    private readonly WorkflowOptions _workflowOptions;
    private readonly ILogger<WorkflowExecutor> _logger;

    private IWorkflowHost Host => _serviceProvider.GetService<IWorkflowHost>();

    public WorkflowExecutor(
        IWorkflowRegistry registry,
        IServiceProvider serviceProvider,
        IScopeProvider scopeProvider,
        IDateTimeProvider datetimeProvider,
        IExecutionResultProcessor executionResultProcessor,
        ILifeCycleEventPublisher publisher,
        ICancellationProcessor cancellationProcessor,
        IExecutionScheduler executionScheduler,
        IOptions<WorkflowOptions> workflowOptions,
        ILogger<WorkflowExecutor> logger)
    {
        _serviceProvider = serviceProvider;
        _scopeProvider = scopeProvider;
        _registry = registry;
        _datetimeProvider = datetimeProvider;
        _publisher = publisher;
        _executionResultProcessor = executionResultProcessor;
        _cancellationProcessor = cancellationProcessor;
        _executionScheduler = executionScheduler;
        _workflowOptions = workflowOptions.Value;
        _logger = logger;
    }

    public async Task<WorkflowExecutorResult> ExecuteAsync(WorkflowInstance workflow, CancellationToken cancellationToken = default)
    {
        var wfResult = new WorkflowExecutorResult();

        var def = _registry.GetDefinition(workflow.WorkflowDefinitionId, workflow.Version);
        if (def == null)
        {
            _logger.LogError("工作流 {WorkflowDefinitionId} 版本 {Version} 未注册", workflow.WorkflowDefinitionId, workflow.Version);
            return wfResult;
        }

        var exePointers = new List<ExecutionPointer>(workflow.ExecutionPointers.Where(x => x.Active && (!x.SleepUntil.HasValue || x.SleepUntil < _datetimeProvider.UtcNow)));

        _cancellationProcessor.ProcessCancellations(workflow, def, wfResult);

        foreach (var pointer in exePointers)
        {
            if (!pointer.Active)
            {
                continue;
            }

            var step = def.Steps.FindById(pointer.StepId);
            if (step == null)
            {
                _logger.LogError("无法在工作流定义中找到步骤 {StepId}", pointer.StepId);

                pointer.SleepUntil = _datetimeProvider.UtcNow.Add(_workflowOptions.ErrorRetryInterval);
                wfResult.Errors.Add(new ExecutionError
                {
                    WorkflowId = workflow.Id,
                    ExecutionPointerId = pointer.Id,
                    ErrorTime = _datetimeProvider.UtcNow,
                    Message = $"无法在工作流定义中找到步骤 {pointer.StepId}"
                });

                continue;
            }

            WorkflowActivity.Enrich(step);

            try
            {
                if (!InitializeStep(workflow, step, wfResult, def, pointer))
                {
                    continue;
                }

                await ExecuteStep(workflow, step, pointer, wfResult, def, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工作流 {WorkflowId} 在步骤 {StepId} 上引发错误,消息: {Message}", workflow.Id, pointer.StepId, ex.Message);

                wfResult.Errors.Add(new ExecutionError
                {
                    WorkflowId = workflow.Id,
                    ExecutionPointerId = pointer.Id,
                    ErrorTime = _datetimeProvider.UtcNow,
                    Message = ex.Message
                });

                _executionResultProcessor.HandleStepException(workflow, def, pointer, step, ex);
                Host.ReportStepError(workflow, step, ex);
            }

            _cancellationProcessor.ProcessCancellations(workflow, def, wfResult);
        }

        ProcessAfterExecutionIteration(workflow, def, wfResult);

        await _executionScheduler.DetermineNextExecutionTimeAsync(workflow, def, cancellationToken);

        using (var scope = _serviceProvider.CreateScope())
        {
            var middlewareRunner = scope.ServiceProvider.GetRequiredService<IWorkflowMiddlewareRunner>();
            await middlewareRunner.RunExecuteMiddlewareAsync(workflow, def, cancellationToken);
        }

        return wfResult;
    }

    private bool InitializeStep(WorkflowInstance workflow, WorkflowStep step, WorkflowExecutorResult wfResult, WorkflowDefinition def, ExecutionPointer pointer)
    {
        switch (step.InitForExecution(wfResult, def, workflow, pointer))
        {
            case ExecutionPipelineDirective.Defer:
                return false;
            case ExecutionPipelineDirective.EndWorkflow:
                workflow.Status = WorkflowStatus.Complete;
                workflow.CompleteTime = _datetimeProvider.UtcNow;
                return false;
        }

        if (pointer.Status != PointerStatus.Running)
        {
            pointer.Status = PointerStatus.Running;

            _publisher.PublishNotification(new StepStarted
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

        if (!pointer.StartTime.HasValue)
        {
            pointer.StartTime = _datetimeProvider.UtcNow;
        }

        return true;
    }

    private async Task ExecuteStep(WorkflowInstance workflow, WorkflowStep step, ExecutionPointer pointer, WorkflowExecutorResult wfResult, WorkflowDefinition def, CancellationToken cancellationToken = default)
    {
        var context = new StepExecutionContext
        {
            Workflow = workflow,
            Step = step,
            PersistenceData = pointer.PersistenceData,
            ExecutionPointer = pointer,
            Item = pointer.ContextItem,
            CancellationToken = cancellationToken
        };

        using var scope = _scopeProvider.CreateScope(context);

        _logger.LogDebug("在工作流 {WorkflowId} 上启动步骤 {StepName}", step.Name, workflow.Id);

        var body = step.ConstructBody(scope.ServiceProvider);
        if (body == null)
        {
            _logger.LogError("无法构造步骤主体 {BodyType}", step.BodyType.ToString());

            pointer.SleepUntil = _datetimeProvider.UtcNow.Add(_workflowOptions.ErrorRetryInterval);
            wfResult.Errors.Add(new ExecutionError
            {
                WorkflowId = workflow.Id,
                ExecutionPointerId = pointer.Id,
                ErrorTime = _datetimeProvider.UtcNow,
                Message = $"无法构造步骤主体 {step.BodyType}"
            });

            return;
        }

        foreach (var input in step.Inputs)
        {
            input.AssignInput(workflow.Data, body, context);
        }

        switch (step.BeforeExecute(wfResult, context, pointer, body))
        {
            case ExecutionPipelineDirective.Defer:
                return;
            case ExecutionPipelineDirective.EndWorkflow:
                workflow.Status = WorkflowStatus.Complete;
                workflow.CompleteTime = _datetimeProvider.UtcNow;
                return;
        }

        var stepExecutor = scope.ServiceProvider.GetRequiredService<IStepExecutor>();
        var result = await stepExecutor.ExecuteStepAsync(context, body, cancellationToken);

        if (result.Proceed)
        {
            foreach (var output in step.Outputs)
            {
                output.AssignOutput(workflow.Data, body, context);
            }
        }

        _executionResultProcessor.ProcessExecutionResult(workflow, def, pointer, step, result, wfResult);

        step.AfterExecute(wfResult, context, result, pointer);
    }

    private static void ProcessAfterExecutionIteration(WorkflowInstance workflow, WorkflowDefinition workflowDef, WorkflowExecutorResult workflowResult)
    {
        var pointers = workflow.ExecutionPointers.Where(x => x.EndTime == null);

        foreach (var pointer in pointers)
        {
            var step = workflowDef.Steps.FindById(pointer.StepId);
            step?.AfterWorkflowIteration(workflowResult, workflowDef, workflow, pointer);
        }
    }
}