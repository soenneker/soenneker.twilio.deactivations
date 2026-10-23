using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Twilio.Deactivations.Abstract;
using Soenneker.Twilio.OpenApiClientUtil.Registrars;
using Soenneker.Utils.HttpClientCache.Registrar;

namespace Soenneker.Twilio.Deactivations.Registrars;

/// <summary>
/// A utility for working with Twilio phone number deactivations.
/// </summary>
public static class TwilioDeactivationsUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="ITwilioDeactivationsUtil"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddTwilioDeactivationsUtilAsSingleton(this IServiceCollection services)
    {
        services.AddTwilioOpenApiClientUtilAsSingleton();
        services.AddHttpClientCacheAsSingleton();
        services.TryAddSingleton<ITwilioDeactivationsUtil, TwilioDeactivationsUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="ITwilioDeactivationsUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddTwilioDeactivationsUtilAsScoped(this IServiceCollection services)
    {
        services.AddTwilioOpenApiClientUtilAsScoped();
        services.AddHttpClientCacheAsSingleton();
        services.TryAddScoped<ITwilioDeactivationsUtil, TwilioDeactivationsUtil>();

        return services;
    }
}


