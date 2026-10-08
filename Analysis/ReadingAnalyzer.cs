using ColdChainMonitor.Models;

namespace ColdChainMonitor.Analysis;

public static class ReadingAnalyzer
{
    public static IReadOnlyList<Alert> Analyze(
        IEnumerable<Reading> readings)
    {
        var alerts = new List<Alert>();

        var groups = readings.GroupBy(
            reading => reading.SensorId,
            StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            string sensorId = group.First().SensorId;

            var ordered = group
                .OrderBy(reading => reading.Timestamp)
                .ToArray();

            for (int i = 0; i < ordered.Length; i++)
            {
                Reading current = ordered[i];

                bool isOutsideRange = IsOutsideSafeRange(current);

                bool isAbruptChange = false;

                if (i > 0)
                {
                    Reading previous = ordered[i - 1];

                    decimal difference = Math.Abs(
                        current.Temperature - previous.Temperature);

                    isAbruptChange = difference > 4.0m;
                }

                if (isOutsideRange || isAbruptChange)
                {
                    alerts.Add(new Alert(
                        SensorId: sensorId,
                        Timestamp: current.Timestamp,
                        StorageClass: current.StorageClass,
                        Temperature: current.Temperature,
                        IsOutsideRange: isOutsideRange,
                        IsAbruptChange: isAbruptChange));
                }
            }
        }

        var sortedAlerts = alerts
            .OrderBy(
                alert => alert.SensorId,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(alert => alert.Timestamp)
            .ToArray();

        return Array.AsReadOnly(sortedAlerts);
    }

    private static bool IsOutsideSafeRange(Reading reading)
    {
        return reading.StorageClass switch
        {
            StorageClass.Cold =>
                reading.Temperature < 2.0m
                || reading.Temperature > 8.0m,

            StorageClass.Frozen =>
                reading.Temperature < -22.0m
                || reading.Temperature > -15.0m,

            _ => throw new ArgumentOutOfRangeException(
                nameof(reading.StorageClass),
                reading.StorageClass,
                "Unknown storage class.")
        };
    }
}