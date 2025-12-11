using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services;

public class WorkflowMiddlewareRunner : IWorkflowMiddlewareRunner
{
    private static readonly WorkflowDelegate NoopWorkflowDelegate = () => Task.CompletedTask;

    private readonly IEnumerable<IWorkflowMiddleware> _middleware;
    private readonly IServiceProvider _serviceProvider;

    public WorkflowMiddlewareRunner(
        IEnumerable<IWorkflowMiddleware> middleware,
        IServiceProvider serviceProvider)
    {
        _middleware = middleware;
        _serviceProvider = serviceProvider;
    }

    public Task RunPreMiddlewareAsync(WorkflowInstance workflow, WorkflowDefinition def)
    {
        return RunWorkflowMiddlewareWithErrorHandlingAsync(
            workflow,
            WorkflowMiddlewarePhase.PreWorkflow,
            middlewareErrorType: null
        );
    }

    public Task RunPostMiddlewareAsync(WorkflowInstance workflow, WorkflowDefinition def)
    {
        return RunWorkflowMiddlewareWithErrorHandlingAsync(
            workflow,
            WorkflowMiddlewarePhase.PostWorkflow,
            def.OnPostMiddlewareError);
    }

    public Task RunExecuteMiddlewareAsync(WorkflowInstance workflow, WorkflowDefinition def)
    {
        return RunWorkflowMiddlewareWithErrorHandlingAsync(
            workflow,
            WorkflowMiddlewarePhase.ExecuteWorkflow,
            def.OnExecuteMiddlewareError);
    }

    public async Task RunWorkflowMiddlewareWithErrorHandlingAsync(WorkflowInstance workflow, WorkflowMiddlewarePhase phase, Type middlewareErrorType)
    {
        try
        {
            var middleware = _middleware.Where(m => m.Phase == phase);
            await RunWorkflowMiddlewareAsync(workflow, middleware);
        }
        catch (Exception exception)
        {
            using var scope = _serviceProvider.CreateScope();
            var errorHandlerType = middlewareErrorType ?? typeof(IWorkflowMiddlewareErrorHandler);
            var typeInstance = scope.ServiceProvider.GetService(errorHandlerType);
            if (typeInstance is IWorkflowMiddlewareErrorHandler handler)
            {
                await handler.HandleAsync(exception);
            }
        }
    }

    private static Task RunWorkflowMiddlewareAsync(WorkflowInstance workflow, IEnumerable<IWorkflowMiddleware> middlewareCollection)
    {
        return middlewareCollection
            .Reverse()
            .Aggregate(
                NoopWorkflowDelegate,
                (previous, middleware) => () => middleware.HandleAsync(workflow, previous))();
    }
}
