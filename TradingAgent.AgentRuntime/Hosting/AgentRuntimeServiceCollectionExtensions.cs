using Microsoft.Extensions.DependencyInjection;

namespace TradingAgent.AgentRuntime.Hosting;

public static class AgentRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddAgentRuntime(this IServiceCollection services)
    {
        // Configure the agent runtime settings
        services.AddOptions<AgentRuntimeConfiguration>()
            .BindConfiguration(nameof(AgentRuntime))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<AgentRuntime>();

        return services;
    }
}