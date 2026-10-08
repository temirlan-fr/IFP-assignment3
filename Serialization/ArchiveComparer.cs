using ColdChainMonitor.Models;

namespace ColdChainMonitor.Serialization;

public static class ArchiveComparer
{
    public static bool HaveSameValues(
        MonitoringArchive original,
        MonitoringArchive restored)
    {
        return original.CreatedAtUtc.EqualsExact(restored.CreatedAtUtc)
            && original.Readings.SequenceEqual(restored.Readings)
            && original.Errors.SequenceEqual(restored.Errors)
            && original.Alerts.SequenceEqual(restored.Alerts);
    }
}