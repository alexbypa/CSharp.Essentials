namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Marks a class as a LoggerHelper sink plugin for compile-time discovery (source generator).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LoggerHelperSinkAttribute : Attribute {
}
