namespace CSharpEssentials.HttpHelper;
public class httpClientOptions {
    public required string Name { get; set; }
    public string? Mock { get; set; }
    public httpClientCertificate? Certificate { get; set; }
    public httpClientRateLimitOptions? RateLimitOptions { get; set; }
}
public class httpClientCertificate {
    public string Path { get; set; }
    public string Password { get; set; }
}
public class httpClientRateLimitOptions {
    public bool AutoReplenishment { get; set; }
    public int PermitLimit { get; set; }
    public int QueueLimit { get; set; }
    public TimeSpan Window { get; set; }
    public int SegmentsPerWindow { get; set; }
    public bool IsEnabled { get; set; }
}
