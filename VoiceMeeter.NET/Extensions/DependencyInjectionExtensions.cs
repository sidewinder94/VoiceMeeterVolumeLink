using Microsoft.Extensions.DependencyInjection;

namespace VoiceMeeter.NET.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddVoiceMeeterClient(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton(_ => VoiceMeeterClient.Create());
        
        return serviceCollection;
    }
}