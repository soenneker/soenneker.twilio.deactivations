using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Twilio.Deactivations.Abstract;

/// <summary>Retrieves daily reports of US phone numbers deactivated by mobile carriers.</summary>
public interface ITwilioDeactivationsUtil
{
    /// <summary>Gets the signed HTTPS download URL for the specified report date.</summary>
    /// <param name="date">The report date, sent to Twilio in YYYY-MM-DD format.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>A signed URL valid for up to two minutes; consume it immediately.</returns>
    /// <exception cref="System.IO.InvalidDataException">Twilio returned no valid HTTPS download URL.</exception>
    ValueTask<Uri> GetDownloadUrl(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Downloads and streams the report one phone number per line, trimming whitespace and skipping empty lines.</summary>
    /// <remarks>Numbers retain the report's formatting and order, including duplicates. Enumeration owns and disposes the download stream.
    /// Twilio API and report download errors propagate to the caller; an unavailable report is not treated as an empty report.</remarks>
    /// <param name="date">The report date.</param>
    /// <param name="cancellationToken">Cancels fetching or reading the report.</param>
    IAsyncEnumerable<string> GetPhoneNumbers(DateOnly date, CancellationToken cancellationToken = default);
}
