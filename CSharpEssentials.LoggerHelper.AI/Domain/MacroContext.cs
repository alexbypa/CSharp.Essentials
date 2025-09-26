namespace CSharpEssentials.LoggerHelper.AI.Domain {
    public class IMacroContext {
        public string action { get; set; }
        public string Query { get; set; }
        public string fileName { get; set; }
        public string system { get; set; }
        public string TraceId { get; set; }
        public DateTimeOffset dtStart { get; set; }
        public int topResultsOnQuery { get; set; }
    }
    // File: CSharpEssentials.LoggerHelper.AI/Domain/RagContext.cs (Nuovo)
    public class RagContext(string QueryText, DateTime? StartTime, DateTime? EndTime) : IMacroContext {
        public string action { get; set; }
        public string Query { get; set; }
        public string fileName { get; set; }
        public string system { get; set; }
        public string TraceId { get; set; }
        public DateTimeOffset dtStart { get; set; }
        public int topResultsOnQuery { get; set; }
    }
    // File: CSharpEssentials.LoggerHelper.AI/Domain/SummarizeContext.cs (Nuovo)
    public class SummarizeContext() : IMacroContext {
        public string action { get; set; }
        public string Query { get; set; }
        public string fileName { get; set; }
        public string system { get; set; }
        public string TraceId { get; set; }
        public DateTimeOffset dtStart { get; set; }
        public int topResultsOnQuery { get; set; }
    }
    // File: CSharpEssentials.LoggerHelper.AI/Domain/CorrelateContext.cs (Nuovo)
    public class CorrelateContext(string TraceId) : IMacroContext {
        public string action { get; set; }
        public string Query { get; set; }
        public string fileName { get; set; }
        public string system { get; set; }
        public string TraceId { get; set; }
        public DateTimeOffset dtStart { get; set; }
        public int topResultsOnQuery { get; set; }
    }
    public class DetectAnomalyContext : IMacroContext {
        public string action { get; set; }
        public string Query { get; set; }
        public string fileName { get; set; }
        public string system { get; set; }
        public string TraceId { get; set; }
        public DateTimeOffset dtStart { get; set; }
        public int topResultsOnQuery { get; set; }
    }
}