using CSharpEssentials.LoggerHelper.AI.Domain;   // fix del typo
using CSharpEssentials.LoggerHelper.AI.Infrastructure;
using CSharpEssentials.LoggerHelper.AI.Ports;
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
        

        TraceFormatterService _formatter;
        string myFormatTemplate = "{TraceId} | Span Name={Name} | duration={Duration:F0}ms | tags={TagsJson}";
        _formatter = new TraceFormatterService(myFormatTemplate);
        var contextBlock = _formatter.Format(traceRecords);


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





public class TraceFormatterService {
    private readonly Func<TEntity, string> _compiledFormatter;
    // Regex per trovare i placeholder come {NomeCampo} o {NomeCampo:Formato}
    private static readonly Regex FormatRegex = new Regex(@"\{(?<propName>\w+)(?::(?<formatSpecifier>[^}]+))?\}", RegexOptions.Compiled);

    public TraceFormatterService(string formatTemplate) {
        _compiledFormatter = CompileFormatter(formatTemplate);
    }

    private Func<TEntity, string> CompileFormatter(string formatTemplate) {
        var entityType = typeof(TEntity); // IDictionary<string, object>
        var parameter = Expression.Parameter(entityType, "t"); // t è IDictionary
        var arguments = new List<Expression>();

        // Ottiene il MethodInfo per la funzione di accesso all'indice: IDictionary<TKey, TValue>.get_Item(TKey key)
        var getItemMethod = entityType.GetMethod("get_Item");

        int index = 0;

        string indexedFormatString = FormatRegex.Replace(formatTemplate, match => {
            var propName = match.Groups["propName"].Value;
            var formatSpecifier = match.Groups["formatSpecifier"].Success ? match.Groups["formatSpecifier"].Value : null;

            // 1. Costruisce l'accesso al valore: t.get_Item("PropName") => t["PropName"]
            var valueAccessExpression = Expression.Call(
                parameter,
                getItemMethod,
                Expression.Constant(propName)
            );

            // Il risultato è di tipo 'object'. string.Format gestisce i tipi in un array di oggetti.
            // Aggiungiamo l'espressione alla lista degli argomenti per string.Format
            arguments.Add(valueAccessExpression);

            // Restituisce l'indice e aggiunge l'eventuale specificatore di formato (es. :F0) per string.Format
            var placeholder = $"{{{index++}{(formatSpecifier != null ? $":{formatSpecifier}" : "")}}}";
            return placeholder;
        });

        // 2. Prepara l'espressione per la chiamata a string.Format(string format, params object[] args)

        // Raggruppa i valori delle proprietà in un array di oggetti (Expression.NewArrayInit)
        var objectArray = Expression.NewArrayInit(typeof(object), arguments);

        // Ottiene il MethodInfo per string.Format(string, object[])
        var formatMethod = typeof(string).GetMethod("Format", new[] { typeof(string), typeof(object[]) });

        // Costruisce la chiamata: string.Format(indexedFormatString, new object[] { arg0, arg1, ... })
        var callExpression = Expression.Call(
            formatMethod,
            Expression.Constant(indexedFormatString),
            objectArray
        );

        // 3. Compila l'Expression Tree in un delegate eseguibile: t => string.Format(...)
        var lambda = Expression.Lambda<Func<TEntity, string>>(callExpression, parameter);

        return lambda.Compile();
    }

    /// <summary>
    /// Esegue la formattazione su un singolo record 'dynamic' di Dapper.
    /// </summary>
    public string Format(dynamic dynamicRecord) {
        // Casting a IDictionary<string, object> per usare il delegate compilato in modo performante.
        return _compiledFormatter((TEntity)dynamicRecord);
    }

    public IEnumerable<string> Format(IEnumerable<dynamic> dynamicRecords) {
        return dynamicRecords.Select(Format);
    }
}