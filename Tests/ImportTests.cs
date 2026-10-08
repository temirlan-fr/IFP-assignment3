using System.Text;
using ColdChainMonitor.Analysis;
using ColdChainMonitor.Importing;
using ColdChainMonitor.Models;
using ColdChainMonitor.Parsing;
using Xunit;

namespace ColdChainMonitor.Tests;

public sealed class ImportTests
{
    [Fact]
    public void SuppliedFile_ProducesExpectedCounts()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "readings.txt");

        using var input = File.OpenRead(path);

        ImportResult result = ReadingImporter.ReadReadings(input);
        var alerts = ReadingAnalyzer.Analyze(result.Readings);

        Assert.Equal(6, result.Readings.Count);
        Assert.Equal(4, result.Errors.Count);
        Assert.Equal(2, alerts.Count);

        Assert.Equal(
            new[] { 7, 8, 9, 10 },
            result.Errors.Select(error => error.LineNumber).ToArray());
    }

    [Fact]
    public void Parser_RejectsUndefinedNumericEnum()
    {
        bool enumParserAccepted = Enum.TryParse<StorageClass>(
            "99",
            out var parsedClass);

        Assert.True(enumParserAccepted);
        Assert.False(
            Enum.IsDefined(typeof(StorageClass), parsedClass));

        bool success = ReadingParser.TryParseReading(
            "S1|2026-09-22T08:00:00Z|99|5.2",
            out var reading,
            out var error);

        Assert.False(success);
        Assert.Null(reading);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Parser_RejectsCommaDecimal()
    {
        bool success = ReadingParser.TryParseReading(
            "S1|2026-09-22T08:00:00Z|Cold|5,5",
            out var reading,
            out var error);

        Assert.False(success);
        Assert.Null(reading);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Importer_RejectsStorageClassChangeForSameSensor()
    {
        string text =
            "S1|2026-09-22T08:00:00Z|Cold|5.2\n" +
            "s1|2026-09-22T08:05:00Z|Frozen|-18.0";

        using var input = new MemoryStream(
            Encoding.UTF8.GetBytes(text));

        ImportResult result = ReadingImporter.ReadReadings(input);

        Reading accepted = Assert.Single(result.Readings);
        ImportError error = Assert.Single(result.Errors);

        Assert.Equal(StorageClass.Cold, accepted.StorageClass);
        Assert.Equal(2, error.LineNumber);
        Assert.Contains("Storage class mismatch", error.Message);
    }

    [Fact]
    public void Parser_AcceptsTrimmedFieldsAndReturnsUtcTimestamp()
    {
        bool success = ReadingParser.TryParseReading(
            " S1 | 2026-09-22T08:00:00Z | cOlD | +5.2 ",
            out var reading,
            out var error);

        Assert.True(success);
        Assert.NotNull(reading);
        Assert.Equal(string.Empty, error);

        Assert.Equal("S1", reading!.SensorId);
        Assert.Equal(StorageClass.Cold, reading.StorageClass);
        Assert.Equal(5.2m, reading.Temperature);

        Assert.Equal(
            new DateTimeOffset(
                2026, 9, 22, 8, 0, 0, TimeSpan.Zero),
            reading.Timestamp);

        Assert.Equal(TimeSpan.Zero, reading.Timestamp.Offset);
    }

    [Fact]
    public void Parser_RejectsInvalidTimestamp()
    {
        bool success = ReadingParser.TryParseReading(
            "S3|bad-date|Cold|3.5",
            out var reading,
            out var error);

        Assert.False(success);
        Assert.Null(reading);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}