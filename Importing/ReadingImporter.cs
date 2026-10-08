using System.Text;
using ColdChainMonitor.Models;
using ColdChainMonitor.Parsing;

namespace ColdChainMonitor.Importing;

public static class ReadingImporter
{
    public static ImportResult ReadReadings(Stream input)
    {
        var readings = new List<Reading>();
        var errors = new List<ImportError>();

        var sensorClasses = new Dictionary<string, StorageClass>(
            StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(
            input,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);

        int lineNumber = 0;
        string? rawLine;

        while ((rawLine = reader.ReadLine()) is not null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            bool success = ReadingParser.TryParseReading(
                rawLine,
                out var reading,
                out var error);

            if (!success || reading is null)
            {
                errors.Add(new ImportError(
                    lineNumber,
                    rawLine,
                    error));

                continue;
            }

            if (sensorClasses.TryGetValue(
                    reading.SensorId,
                    out StorageClass previousClass))
            {
                if (reading.StorageClass != previousClass)
                {
                    errors.Add(new ImportError(
                        lineNumber,
                        rawLine,
                        $"Storage class mismatch: expected {previousClass}, got {reading.StorageClass}."));

                    continue;
                }
            }
            else
            {
                sensorClasses.Add(
                    reading.SensorId,
                    reading.StorageClass);
            }

            readings.Add(reading);
        }

        return new ImportResult(readings, errors);
    }
}