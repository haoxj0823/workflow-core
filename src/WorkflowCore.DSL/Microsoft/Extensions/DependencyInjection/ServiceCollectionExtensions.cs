using WorkflowCore.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowDSL(this IServiceCollection services)
    {
        services.AddTransient<ITypeResolver, TypeResolver>();
        services.AddTransient<IDefinitionLoader, DefinitionLoader>();
        return services;
    }
}
