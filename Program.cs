using ColdChainMonitor.Importing;
using ColdChainMonitor.Models;

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