namespace ColdChainMonitor.Models;

public sealed record MonitoringArchive
{
    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<Reading> Readings { get; }
    public IReadOnlyList<ImportError> Errors { get; }
    public IReadOnlyList<Alert> Alerts { get; }

    public MonitoringArchive(
        DateTimeOffset createdAtUtc,
        IReadOnlyList<Reading> readings,
        IReadOnlyList<ImportError> errors,
        IReadOnlyList<Alert> alerts)
    {
        CreatedAtUtc = createdAtUtc;

        Readings = Array.AsReadOnly(readings.ToArray());
        Errors = Array.AsReadOnly(errors.ToArray());
        Alerts = Array.AsReadOnly(alerts.ToArray());
    }
}