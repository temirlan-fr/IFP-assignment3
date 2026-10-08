namespace ColdChainMonitor.Models;

public sealed record ImportResult
{
    public IReadOnlyList<Reading> Readings { get; }
    public IReadOnlyList<ImportError> Errors { get; }

    public ImportResult(
        IEnumerable<Reading> readings,
        IEnumerable<ImportError> errors)
    {
        Readings = Array.AsReadOnly(readings.ToArray());
        Errors = Array.AsReadOnly(errors.ToArray());
    }
}