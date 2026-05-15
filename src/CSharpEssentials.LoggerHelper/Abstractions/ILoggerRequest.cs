namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Standard structured fields for transactional logging context.
/// </summary>
public interface ILoggerRequest {
    string IdTransaction { get; }
    string Action { get; }
    string ApplicationName { get; }
}

/// <summary>
/// Alias for <see cref="ILoggerRequest"/> used by HttpHelper and legacy integrations.
/// </summary>
public interface IRequest : ILoggerRequest;
