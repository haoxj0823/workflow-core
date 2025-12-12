using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Models;

namespace WorkflowCore.Services.Middleware;

public class WorkflowMiddlewareRunner : IWorkflowMiddlewareRunner
{
    private static readonly WorkflowDelegate NoopWorkflowDelegate = cancellationToken => Task.CompletedTask;

    private readonly IEnumerable<IWorkflowMiddleware> _middleware;
    private readonly IServiceProvider _serviceProvider;

    public WorkflowMiddlewareRunner(
        IEnumerable<IWorkflowMiddleware> middleware,
        IServiceProvider serviceProvider)
    {
        _middleware = middleware;
        _serviceProvider = serviceProvider;
    }

    public Task RunPreMiddlewareAsync(WorkflowInstance workflow, WorkflowDefinition def, CancellationToken cancellationToken = default)
    {
        return RunWorkflowMiddlewareWithErrorHandlingAsync(
            workflow,
            WorkflowMiddlewarePhase.PreWorkflow,
            middlewareErrorType: null,
            cancellationToken
        );
    }

    public Task RunPostMiddlewareAsync(WorkflowInstance workflow, WorkflowDefinition def, CancellationToken cancellationToken = default)
    {
        return RunWorkflowMiddlewareWithErrorHandlingAsync(
            workflow,
            WorkflowMiddlewarePhase.PostWorkflow,
            def.OnPostMiddlewareError,
            cancellationToken);
    }

    public Task RunExecuteMiddlewareAsync(WorkflowInstance workflow, WorkflowDefinition def, CancellationToken cancellationToken = default)
    {
        return RunWorkflowMiddlewareWithErrorHandlingAsync(
            workflow,
            WorkflowMiddlewarePhase.ExecuteWorkflow,
            def.OnExecuteMiddlewareError,
            cancellationToken);
    }

    public async Task RunWorkflowMiddlewareWithErrorHandlingAsync(WorkflowInstance workflow, WorkflowMiddlewarePhase phase, Type middlewareErrorType, CancellationToken cancellationToken = default)
    {
        try
        {
            var middleware = _middleware.Where(m => m.Phase == phase);
            await RunWorkflowMiddlewareAsync(workflow, middleware, cancellationToken);
        }
        catch (Exception exception)
        {
            using var scope = _serviceProvider.CreateScope();
            var errorHandlerType = middlewareErrorType ?? typeof(IWorkflowMiddlewareErrorHandler);
            var typeInstance = scope.ServiceProvider.GetService(errorHandlerType);
            if (typeInstance is IWorkflowMiddlewareErrorHandler handler)
            {
                await handler.HandleAsync(exception, cancellationToken);
            }
        }
    }

    private static async Task RunWorkflowMiddlewareAsync(WorkflowInstance workflow, IEnumerable<IWorkflowMiddleware> middlewareCollection, CancellationToken cancellationToken = default)
    {
        var middlewareChain = middlewareCollection
            .Reverse()
            .Aggregate(
                NoopWorkflowDelegate,
                (previous, middleware) => cancellationToken => middleware.HandleAsync(workflow, previous, cancellationToken));

        await middlewareChain(cancellationToken);
    }
}
