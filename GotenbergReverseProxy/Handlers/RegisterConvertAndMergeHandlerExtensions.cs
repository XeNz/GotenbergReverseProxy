using System.Reflection;

namespace GotenbergReverseProxy.Handlers;

internal static class RegisterConvertAndMergeHandlerExtensions
{
    internal static IServiceCollection RegisterConvertAndMergeHandler(this IServiceCollection services)
    {
        services.AddScoped<IConvertAndMergeHandler, ConvertAndMergeWebhookHandler>();
        return services;
    }
}