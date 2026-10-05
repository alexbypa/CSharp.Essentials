namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Marker for LoggerHelper sink plugins. Not required for discovery; reserved for future tooling.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LoggerHelperSinkAttribute : Attribute {
}
