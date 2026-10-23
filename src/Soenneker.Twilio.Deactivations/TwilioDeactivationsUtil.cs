using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Twilio.Deactivations.Abstract;
using Soenneker.Twilio.OpenApiClient;
using Soenneker.Twilio.OpenApiClientUtil.Abstract;

namespace Soenneker.Twilio.Deactivations;

public sealed class TwilioDeactivationsUtil : ITwilioDeactivationsUtil
{
    private readonly ITwilioOpenApiClientUtil _clientUtil;
    private readonly IHttpClientCache _httpClientCache;

    public TwilioDeactivationsUtil(ITwilioOpenApiClientUtil clientUtil, IHttpClientCache httpClientCache)
    {
        _clientUtil = clientUtil;
        _httpClientCache = httpClientCache;
    }

    public async ValueTask<Uri> GetDownloadUrl(DateOnly date, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TwilioOpenApiClient client = await _clientUtil.Get(cancellationToken).NoSync();
        string url = "https://messaging.twilio.com/v1/Deactivations?Date=" +
                     date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        // GetAsync returns no response value (SendNoContentAsync). Capture the native response
        // to inspect the redirect location, final request URI, or JSON body.
        var handler = new Microsoft.Kiota.Abstractions.NativeResponseHandler();
        await client.Twilio_messaging_v1.V1.Deactivations.WithUrl(url).GetAsync(
            config => config.Options.Add(new Microsoft.Kiota.Abstractions.ResponseHandlerOption
                { ResponseHandler = handler }), cancellationToken).NoSync();
        using HttpResponseMessage response = handler.Value as HttpResponseMessage ??
                                             throw new InvalidDataException(
                                                 "Twilio returned an empty deactivations response.");
        // Twilio can return a 307 to the signed report. Depending on transport settings,
        // the response may already be the text file, so recover its final URI before parsing JSON.
        if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308 && response.Headers.Location is { } location)
            return ValidateDownloadUrl(new Uri(new Uri(url), location));
        response.EnsureSuccessStatusCode();
        if (response.RequestMessage?.RequestUri is { } finalUri &&
            !string.Equals(finalUri.Host, new Uri(url).Host, StringComparison.OrdinalIgnoreCase))
            return ValidateDownloadUrl(finalUri);
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).NoSync();
        using JsonDocument document =
            await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).NoSync();
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("redirect_to", out JsonElement redirect) ||
            redirect.ValueKind != JsonValueKind.String ||
            !Uri.TryCreate(redirect.GetString(), UriKind.Absolute, out Uri? downloadUrl) ||
            downloadUrl.Scheme != Uri.UriSchemeHttps || downloadUrl.UserInfo.HasContent())
            throw new InvalidDataException("Twilio returned no valid HTTPS deactivations download URL.");

        return downloadUrl;
    }

    private static Uri ValidateDownloadUrl(Uri url)
    {
        if (!url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttps || url.UserInfo.HasContent())
            throw new InvalidDataException("Twilio returned no valid HTTPS deactivations download URL.");

        return url;
    }
    public async IAsyncEnumerable<string> GetPhoneNumbers(DateOnly date,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Uri url = await GetDownloadUrl(date, cancellationToken).NoSync();
        // Signed report downloads must not use the authenticated Twilio request adapter.
        HttpClient httpClient = await _httpClientCache.Get(nameof(TwilioDeactivationsUtil), cancellationToken).NoSync();
        using HttpResponseMessage response =
            await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).NoSync();
        response.EnsureSuccessStatusCode();
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).NoSync();
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).NoSync() is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string number = line.Trim();
            if (number.HasContent())
                yield return number;
        }
    }
}

