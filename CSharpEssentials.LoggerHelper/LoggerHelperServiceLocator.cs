namespace CSharpEssentials.LoggerHelper;
/// <summary>
/// A simple service locator used by LoggerHelper to resolve services dynamically at runtime.
/// This static class provides access to the application's IServiceProvider instance,
/// allowing you to resolve any registered service, such as custom enrichers or loggers.
/// </summary>
public static class LoggerHelperServiceLocator {
    /// <summary>
    /// Holds the application's IServiceProvider instance, typically assigned during application startup.
    /// </summary>
    public static IServiceProvider? Instance { get; set; }
    /// <summary>
    /// Resolves and returns an instance of the requested service type (T).
    /// If the service is not found, returns null.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve.</typeparam>
    /// <returns>An instance of the requested service type, or null if not registered.</returns>
    public static T? GetService<T>() where T : class {
        return Instance?.GetService(typeof(T)) as T;
    }
}