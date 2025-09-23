namespace CSharpEssentials.LoggerHelper.AI.Domain;
public class SQLLMModels {
    public required string action { get; set; }
    public required List<SQLLMModelContent> contents { get; set; }
}
public class SQLLMModelContent {
    public required string content { get; set; }
    public required string fileName { get; set; }
}