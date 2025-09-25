using CSharpEssentials.LoggerHelper.AI.Domain;   // fix del typo
using CSharpEssentials.LoggerHelper.AI.Infrastructure;
using CSharpEssentials.LoggerHelper.AI.Ports;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using static Dapper.SqlMapper;
using TEntity = System.Collections.Generic.IDictionary<string, object>;

namespace CSharpEssentials.LoggerHelper.AI.Application;

public sealed class CorrelateTraceAction : ILogMacroAction<CorrelateContext> {
    //private readonly ILogRepository _logs;
    //private readonly ITraceRepository<TraceRecord> _traces;
    private readonly ISqlQueryWrapper _sqlQueryWrapper;
    public string Name => "CorrelateTrace";
    public Type ContextType => typeof(CorrelateContext);

    private readonly ILlmChat _llm;
    private readonly List<SQLLMModels> _sQLLMModels;
    public CorrelateTraceAction(ISqlQueryWrapper sqlQueryWrapper , ILlmChat llm, List<SQLLMModels> sQLLMModels) {
        _sqlQueryWrapper = sqlQueryWrapper;
        _llm = llm;
        _sQLLMModels = sQLLMModels;
    }
    public bool CanExecute(IMacroContext ctx) => !string.IsNullOrEmpty(ctx.TraceId);
    public async Task<MacroResult> ExecuteAsync(IMacroContext ctx, CancellationToken ct = default) {
        // _traces.GetRecentAsync ritorna Task<IReadOnlyList<TraceRecord>>
        // var recent = await _traces.GetRecentAsync(50, ct);
        var sqlQuery = _sQLLMModels.FirstOrDefault(a => a.action == Name).contents.FirstOrDefault(a => a.fileName == ctx.fileName)?.content;

        dynamic traceRecords = await _sqlQueryWrapper.QueryAsync(sqlQuery, new {traceid = ctx.TraceId});

        //// scegli il predicato coerente col tuo model (es. Anomaly == true)
        //var hit = recent.FirstOrDefault(t => t.Anomaly == true);
        
        string Template = "{TraceId} | Span Name={Name} | duration={Duration:F0}ms | tags={TagsJson}";
        var contextBlock =  string.Join("\n---\n", TraceFormatter.FormatRecords(traceRecords, Template));

        //var lines = traceRecords.Select(t => $"{t.TraceId} | Span Name={t.Name} | duration={t.Duration:F0}ms | tags={t.TagsJson}");
        //var contextBlock = string.Join("\n---\n", lines.Select(h => h));

        var messages = new[]{
            new ChatPromptMessage("system", ctx.system),
            new ChatPromptMessage("assistant", $"CONTEXT:\n{contextBlock}"),
            new ChatPromptMessage("user", $"Question: {ctx.Query}")
        };
        var answer = await _llm.ChatAsync(messages);

        return new MacroResult(Name, answer);
        
    }
}





public static class TraceFormatter {
    // {Prop} oppure {Prop:Formato} -> es. {Duration:F0}
    private static readonly Regex FormatRegex =
        new(@"\{(?<prop>\w+)(?::(?<fmt>[^}]+))?\}", RegexOptions.Compiled);

    public static IEnumerable<string> FormatRecords(IEnumerable<dynamic> records, string template) {
        if (records is null)
            throw new ArgumentNullException(nameof(records));
        if (template is null)
            throw new ArgumentNullException(nameof(template));

        var propOrder = new List<string>();

        // Costruisce il template indicizzato per string.Format e registra l'ordine dei campi
        string indexedTemplate = FormatRegex.Replace(template, m => {
            var prop = m.Groups["prop"].Value;
            var fmt = m.Groups["fmt"].Success ? ":" + m.Groups["fmt"].Value : string.Empty;
            propOrder.Add(prop);
            return "{" + (propOrder.Count - 1) + fmt + "}";
        });

        foreach (var r in records) {
            // Supporta ExpandoObject, Dapper row, e anonymous dynamics via IDictionary<string, object>
            if (r is not IDictionary<string, object> dict)
                throw new ArgumentException("Ogni record dynamic deve essere castabile a IDictionary<string, object>.");

            var args = new object?[propOrder.Count];
            for (int i = 0; i < propOrder.Count; i++) {
                dict.TryGetValue(propOrder[i], out var val);
                args[i] = val; // null ok, string.Format gestisce null
            }

            yield return string.Format(CultureInfo.InvariantCulture, indexedTemplate, args);
        }
    }
}
