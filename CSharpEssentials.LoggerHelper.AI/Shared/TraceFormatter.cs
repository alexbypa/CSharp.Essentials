using System.Globalization;
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
}