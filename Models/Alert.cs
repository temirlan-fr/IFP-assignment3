namespace ColdChainMonitor.Models;

public sealed record Alert(
    string SensorId,
    DateTimeOffset Timestamp,
    StorageClass StorageClass,
    decimal Temperature,
    bool IsOutsideRange,
    bool IsAbruptChange);