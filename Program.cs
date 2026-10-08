using ColdChainMonitor.Importing;
using ColdChainMonitor.Models;
using ColdChainMonitor.Analysis;
using ColdChainMonitor.Serialization;

ImportResult result;

using (FileStream input = File.OpenRead("readings.txt"))
{
    result = ReadingImporter.ReadReadings(input);
}

Console.WriteLine("Cold Chain Monitor");
Console.WriteLine($"Valid readings: {result.Readings.Count}");
Console.WriteLine($"Import errors: {result.Errors.Count}");

Console.WriteLine();
Console.WriteLine("VALID READINGS");

foreach (var reading in result.Readings)
{
    Console.WriteLine(FormattableString.Invariant(
        $"{reading.SensorId} | {reading.Timestamp:yyyy-MM-dd'T'HH:mm:ss'Z'} | {reading.StorageClass} | {reading.Temperature:F1}"));
}

Console.WriteLine();
Console.WriteLine("IMPORT ERRORS");

foreach (var error in result.Errors)
{
    Console.WriteLine($"Line {error.LineNumber}: {error.Message}");
    Console.WriteLine($"Raw: {error.RawLine}");
    Console.WriteLine();
}

var alerts = ReadingAnalyzer.Analyze(result.Readings);

Console.WriteLine("ALERTS");
Console.WriteLine($"Total alerts: {alerts.Count}");

foreach (var alert in alerts)
{
    Console.WriteLine(FormattableString.Invariant(
        $"{alert.SensorId} | {alert.Timestamp:HH:mm} UTC | {alert.Temperature:F1} C"));

    Console.WriteLine($"Outside range: {alert.IsOutsideRange}");
    Console.WriteLine($"Abrupt change: {alert.IsAbruptChange}");
    Console.WriteLine();
}

var archive = new MonitoringArchive(
    createdAtUtc: DateTimeOffset.UtcNow,
    readings: result.Readings,
    errors: result.Errors,
    alerts: alerts);

using (FileStream output = File.Create("archive.json"))
{
    ArchiveSerializer.WriteArchive(output, archive);
}

Console.WriteLine("Archive saved to archive.json.");

MonitoringArchive restoredFromFile;

using (FileStream input = File.OpenRead("archive.json"))
{
    restoredFromFile = ArchiveSerializer.ReadArchive(input);
}

bool fileRoundTripIsValid = ArchiveComparer.HaveSameValues(
    archive,
    restoredFromFile);

if (!fileRoundTripIsValid)
{
    throw new InvalidOperationException(
        "File round trip changed archive values.");
}

Console.WriteLine("File round trip: OK");

using (var memory = new MemoryStream())
{
    ArchiveSerializer.WriteArchive(memory, archive);

    memory.Position = 0;

    MonitoringArchive restoredFromMemory =
        ArchiveSerializer.ReadArchive(memory);

    bool memoryRoundTripIsValid = ArchiveComparer.HaveSameValues(
        archive,
        restoredFromMemory);

    if (!memoryRoundTripIsValid)
    {
        throw new InvalidOperationException(
            "Memory round trip changed archive values.");
    }

    Console.WriteLine("Memory round trip: OK");
}