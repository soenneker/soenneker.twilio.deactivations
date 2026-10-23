using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Soenneker.Extensions.String;
using System.Threading.Tasks;
using Soenneker.Twilio.Deactivations.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Twilio.Deactivations.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class TwilioDeactivationsUtilTests : HostedUnitTest
{
    private readonly ITwilioDeactivationsUtil _util;

    public TwilioDeactivationsUtilTests(Host host) : base(host)
    {
        _util = Resolve<ITwilioDeactivationsUtil>(true);
    }

    [Test]
    [Category("Integration")]
    [Explicit]
    public async ValueTask GetPhoneNumbers_downloads_real_report()
    {
        var config = Resolve<IConfiguration>();
        if (config["Twilio:ApiKey"].IsNullOrWhiteSpace() || config["Twilio:ApiSecret"].IsNullOrWhiteSpace())
            throw new InvalidOperationException("Set Twilio__ApiKey and Twilio__ApiSecret to run the live deactivations integration test.");

        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        // A historical report avoids the publication delay of today's deactivations.
        var date = new DateOnly(2026, 8, 13);
        var count = 0;

        List<string> numbers = await _util.GetPhoneNumbers(date, timeout.Token).ToListAsync(cancellationToken: timeout.Token);

        foreach (string number in numbers)
        {
            ReadOnlyMemory<char> digits = number.AsMemory(number.StartsWith('+') ? 1 : 0);
            // Reports can contain short numeric entries; the utility preserves Twilio's data.
            if (digits.IsEmpty || digits.Span.ContainsAnyExceptInRange('0', '9'))
                throw new InvalidOperationException("The report contained an invalid phone number.");

            count++;
        }

        if (count == 0)
            throw new InvalidOperationException("Expected the historical deactivations report to contain phone numbers.");
    }
    [Test]
    public async ValueTask GetDownloadUrl_cancels_before_creating_authenticated_client()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        try
        {
            await _util.GetDownloadUrl(new DateOnly(2026, 10, 20), cancellation.Token);
        }
        catch (OperationCanceledException exception) when (exception.CancellationToken == cancellation.Token)
        {
            return;
        }

        throw new InvalidOperationException("Expected cancellation before any network request.");
    }

    [Test]
    public async ValueTask GetPhoneNumbers_honors_enumerator_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        try
        {
            await foreach (string number in _util.GetPhoneNumbers(new DateOnly(2026, 10, 20)).WithCancellation(cancellation.Token))
                throw new InvalidOperationException("A canceled enumeration must not return numbers.");
        }
        catch (OperationCanceledException exception) when (exception.CancellationToken == cancellation.Token)
        {
            return;
        }

        throw new InvalidOperationException("Expected cancellation before any network request.");
    }
}


