using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Soenneker.TestHosts.Unit;
using Soenneker.Utils.Test;
using Soenneker.Twilio.Deactivations.Registrars;

namespace Soenneker.Twilio.Deactivations.Tests;

public sealed class Host : UnitTestHost
{
    public override Task InitializeAsync()
    {
        SetupIoC(Services);

        return base.InitializeAsync();
    }

    private static void SetupIoC(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: false);
        });

        IConfiguration config = new ConfigurationBuilder()
            .AddConfiguration(TestUtil.BuildConfig())
            .AddEnvironmentVariables()
            .Build();
        services.AddSingleton(config);

        services.AddTwilioDeactivationsUtilAsScoped();
    }
}

