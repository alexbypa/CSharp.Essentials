namespace CSharpEssentials.LoggerHelper.model;
public class LoadedSinkInfo {
    public string SinkName { get; set; } = string.Empty;
    public List<string> Levels { get; set; } = new();
}