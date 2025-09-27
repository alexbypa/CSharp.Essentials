using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpEssentials.LoggerHelper.AI.Shared;
public static class TraceFormatter {
    private static readonly Regex FormatRegex = new(@"\{(?<prop>\w+)(?::(?<fmt>[^}]+))?\}", RegexOptions.Compiled);

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
    public static string FormatMetrics(
         List<(DateTimeOffset Time, double Value)> series,
         string metricName,
         string metricUnit,
         int sampleSize = 5) // number of intermediate samples to show
     {
        if (series == null || !series.Any()) {
            return $"No {metricName} data available for the requested time window.";
        }

        var sb = new StringBuilder();
        var values = series.Select(p => p.Value).ToList();

        // query of statistics
        var min = values.Min();
        var max = values.Max();
        var avg = values.Average();
        var variance = values.Sum(v => Math.Pow(v - avg, 2)) / values.Count;
        var stdDev = Math.Sqrt(variance);

        var startTime = series.Min(p => p.Time);
        var endTime = series.Max(p => p.Time);

        sb.AppendLine($"--- METRIC: {metricName.ToUpper()} ANALYSIS (Unit: {metricUnit}) ---");
        sb.AppendLine($"Time Window: {startTime:HH:mm:ss} to {endTime:HH:mm:ss}");
        sb.AppendLine($"Total Data Points: {series.Count}");

        sb.AppendLine("\nSUMMARY STATISTICS:");
        sb.AppendLine($"- Minimum Value: {min:F2} {metricUnit}");
        sb.AppendLine($"- Maximum Value: {max:F2} {metricUnit}");
        sb.AppendLine($"- Average Value: {avg:F2} {metricUnit}");
        sb.AppendLine($"- Standard Deviation (Variability): {stdDev:F2} {metricUnit}");

        // Campionamento Neutrale: Mostra inizio, fine, min, max e qualche punto intermedio
        var sampledPoints = new List<(DateTimeOffset Time, double Value)> {
            // 1. Inizio e Fine (per vedere l'andamento temporale)
            series.First(),
            series.Last(),

            // 2. Minimo e Massimo (i punti di interesse primario)
            series.First(p => p.Value == min),
            series.First(p => p.Value == max)
        };

        // 3. Punti intermedi (campionamento per mostrare il trend)
        // Usa un campionamento equidistante o semplice Take/Skip
        var internalSamples = series.Skip(1).Take(series.Count - 2).Where((p, i) => i % (series.Count / sampleSize + 1) == 0).Take(sampleSize);
        sampledPoints.AddRange(internalSamples);

        sb.AppendLine("\nDATA SAMPLE (Key Points & Trend):");

        // Ordina e rimuovi duplicati prima di stampare
        var uniqueSampledPoints = sampledPoints
            .DistinctBy(p => p.Time.Ticks)
            .OrderBy(p => p.Time);

        foreach (var p in uniqueSampledPoints) {
            // Etichetta i punti Min/Max per l'LLM
            string label = "";
            if (p.Value == max)
                label = " <--- MAX";
            else if (p.Value == min)
                label = " <--- MIN";

            sb.AppendLine($"  {p.Time:HH:mm:ss} | {p.Value:F2} {metricUnit}{label}");
        }

        return sb.ToString();
    }
}