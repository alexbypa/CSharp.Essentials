using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using OpenTelemetry;
using System.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Telemetry.Exporters;
/// <summary>
/// Exports completed OpenTelemetry Activities (spans) to a PostgreSQL database.
/// Creates TraceEntry records for each Activity with properties like TraceId, SpanId, timestamps, and tags.
/// </summary>
public class PostgreSqlTraceExporter : BaseExporter<Activity> {
    private readonly ITraceEntryFactory _factory;
    private readonly ITraceEntryRepository _repository;

    public PostgreSqlTraceExporter(ITraceEntryFactory factory, ITraceEntryRepository repository) {
        _factory = factory;
        _repository = repository;
    }

    public override ExportResult Export(in Batch<Activity> batch) {
        var entries = new List<TraceEntry>();
        foreach (var activity in batch) {
            var entry = _factory.Create(activity);
            entries.Add(entry);
        }

        try {
            _repository.Save(entries);
        } catch (Exception ex) {
            Debug.WriteLine($"Error saving trace entries: {ex}");
        }

        return ExportResult.Success;
    }
}