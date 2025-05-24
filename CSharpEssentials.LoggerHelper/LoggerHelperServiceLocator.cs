namespace CSharpEssentials.LoggerHelper;
public static class LoggerHelperServiceLocator {
    public static IServiceProvider? Instance { get; set; }

    public static T? GetService<T>() where T : class {
        return Instance?.GetService(typeof(T)) as T;
    }
}