using ColdChainMonitor.Parsing;

string[] examples =
{
    "S1|2026-09-22T08:10:00Z|Cold|5.2",
    "S3|bad-date|Cold|3.5",
    "S2|2026-09-22T08:11:00Z|99|-17.0",
    "S1|2026-09-22T08:20:00Z|Cold|5,5"
};

foreach (string line in examples)
{
    Console.WriteLine($"Input: {line}");

    bool success = ReadingParser.TryParseReading(
        line,
        out var reading,
        out var error);

    if (success && reading is not null)
    {
        Console.WriteLine(FormattableString.Invariant(
            $"OK: {reading.SensorId}, {reading.StorageClass}, {reading.Temperature} C"));
    }
    else
    {
        Console.WriteLine($"ERROR: {error}");
    }

    Console.WriteLine();
}