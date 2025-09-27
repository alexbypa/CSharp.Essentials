namespace CSharpEssentials.LoggerHelper.AI.Domain {
    public class MacroContextBase {
        public string action { get; set; }
        public string Query { get; set; }
        public string fileName { get; set; }
        public string system { get; set; }
        public string TraceId { get; set; }
        public DateTimeOffset dtStart { get; set; }
        public DateTimeOffset dtEnd { get; set; }
        public int topResultsOnQuery { get; set; }
    }
    // File: CSharpEssentials.LoggerHelper.AI/Domain/RagContext.cs (Nuovo)
    public class RagContext(string QueryText, DateTime? StartTime, DateTime? EndTime) : MacroContextBase { }
    // File: CSharpEssentials.LoggerHelper.AI/Domain/SummarizeContext.cs (Nuovo)
    public class SummarizeContext() : MacroContextBase { }
    // File: CSharpEssentials.LoggerHelper.AI/Domain/CorrelateContext.cs (Nuovo)
    public class CorrelateContext(string TraceId) : MacroContextBase { }
    public class DetectAnomalyContext : MacroContextBase { }
}