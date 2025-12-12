using WorkflowCore.Models;
using WorkflowCore.Services.Middleware;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflow(this IServiceCollection services, Action<WorkflowOptions> setupAction = null)
    {
        var options = new WorkflowOptions();
        setupAction?.Invoke(options);

        return services;
    }

    /// <summary>
    /// Adds a middleware that will run around the execution of a workflow step.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="factory">Optionally configure using your own factory.</param>
    /// <typeparam name="TMiddleware">The type of middleware.
    /// It must implement <see cref="IWorkflowStepMiddleware"/>.</typeparam>
    /// <returns>The services collection for chaining.</returns>
    public static IServiceCollection AddWorkflowStepMiddleware<TMiddleware>(
        this IServiceCollection services,
        Func<IServiceProvider, TMiddleware> factory = null)
        where TMiddleware : class, IWorkflowStepMiddleware =>
            factory == null
                ? services.AddTransient<IWorkflowStepMiddleware, TMiddleware>()
                : services.AddTransient<IWorkflowStepMiddleware, TMiddleware>(factory);

    /// <summary>
    /// Adds a middleware that will run either before a workflow is kicked off or after
    /// a workflow completes. Specify the phase of the workflow execution process that
    /// you want to execute this middleware using <see cref="IWorkflowMiddleware.Phase"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="factory">Optionally configure using your own factory.</param>
    /// <typeparam name="TMiddleware">The type of middleware.
    /// It must implement <see cref="IWorkflowMiddleware"/>.</typeparam>
    /// <returns>The services collection for chaining.</returns>
    public static IServiceCollection AddWorkflowMiddleware<TMiddleware>(
        this IServiceCollection services,
        Func<IServiceProvider, TMiddleware> factory = null)
        where TMiddleware : class, IWorkflowMiddleware =>
            factory == null
                ? services.AddTransient<IWorkflowMiddleware, TMiddleware>()
                : services.AddTransient<IWorkflowMiddleware, TMiddleware>(factory);
}
