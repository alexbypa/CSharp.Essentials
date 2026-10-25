namespace CSharpEssentials.HttpHelper;

/// <summary>Why HttpHelper produced a synthetic response instead of a real one.</summary>
public enum SyntheticResponseReason {
    /// <summary>addTimeout or HttpClient.Timeout expired before a response arrived.</summary>
    Timeout,
    /// <summary>HttpRequestException (DNS, connection refused, connection reset, TLS...): no response was received.</summary>
    TransportFailure,
    /// <summary>Any other exception while sending the request.</summary>
    InternalError,
    /// <summary>The rate limiter refused the lease; the request was never sent.</summary>
    RateLimited
}

/// <summary>Extension methods to tell a synthetic HttpHelper response apart from a real one.</summary>
public static class HttpResponseMessageExtensions {
    internal static readonly HttpRequestOptionsKey<SyntheticResponseReason> SyntheticMarkerKey = new("CSharpEssentials.HttpHelper.SyntheticResponse");

    /// <summary>True when HttpHelper built this response itself instead of returning what the server sent (or the request was never sent, e.g. rate limited).</summary>
    public static bool IsSynthetic(this HttpResponseMessage response) => response.GetSyntheticReason() is not null;

    /// <summary>True only when nothing was received and the outcome is unknown: timeout (408) or a transport-level failure (502). A caller must not assume the request had no side effect.</summary>
    public static bool IsTransportFailure(this HttpResponseMessage response) {
        var reason = response.GetSyntheticReason();
        return reason == SyntheticResponseReason.Timeout || reason == SyntheticResponseReason.TransportFailure;
    }

    /// <summary>The synthetic reason, or null for a real response from the server.</summary>
    public static SyntheticResponseReason? GetSyntheticReason(this HttpResponseMessage response) =>
        response.RequestMessage?.Options.TryGetValue(SyntheticMarkerKey, out var reason) == true ? reason : null;
}
