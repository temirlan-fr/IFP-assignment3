using System.Text;
using System.Text.Json;
using ColdChainMonitor.Analysis;
using ColdChainMonitor.Importing;
using ColdChainMonitor.Models;
using ColdChainMonitor.Serialization;
using Xunit;

namespace ColdChainMonitor.Tests;

public sealed class StreamAndJsonTests
{
    private static MonitoringArchive CreateArchive()
    {
        Reading[] readings =
        {
            new Reading(
                "S1",
                new DateTimeOffset(
                    2026, 9, 22, 8, 0, 0, TimeSpan.Zero),
                StorageClass.Cold,
                4.0m),

            new Reading(
                "S1",
                new DateTimeOffset(
                    2026, 9, 22, 8, 5, 0, TimeSpan.Zero),
                StorageClass.Cold,
                8.7m)
        };

        ImportError[] errors =
        {
            new ImportError(
                3,
                "S3|bad-date|Cold|3.5",
                "Invalid timestamp.")
        };

        var alerts = ReadingAnalyzer.Analyze(readings);

        return new MonitoringArchive(
            new DateTimeOffset(
                2026, 9, 22, 9, 0, 0, TimeSpan.Zero),
            readings,
            errors,
            alerts);
    }

    [Fact]
    public void ReadReadings_LeavesCallerStreamOpen()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(
            "S1|2026-09-22T08:00:00Z|Cold|4.0");

        using var stream = new MemoryStream(bytes);

        ReadingImporter.ReadReadings(stream);

        Assert.True(stream.CanRead);
        Assert.True(stream.CanSeek);

        stream.Position = 0;
        Assert.Equal((int)'S', stream.ReadByte());
    }

    [Fact]
    public void WriteArchive_LeavesCallerStreamOpen()
    {
        MonitoringArchive archive = CreateArchive();

        using var stream = new MemoryStream();

        ArchiveSerializer.WriteArchive(stream, archive);

        Assert.True(stream.CanWrite);
        Assert.True(stream.Length > 0);

        long previousLength = stream.Length;
        stream.WriteByte((byte)' ');

        Assert.Equal(previousLength + 1, stream.Length);
    }

    [Fact]
    public void ReadArchive_LeavesCallerStreamOpen()
    {
        using var stream = new MemoryStream();

        ArchiveSerializer.WriteArchive(stream, CreateArchive());
        stream.Position = 0;

        ArchiveSerializer.ReadArchive(stream);

        Assert.True(stream.CanRead);
        Assert.True(stream.CanSeek);

        stream.Position = 0;
        Assert.Equal((int)'{', stream.ReadByte());
    }

    [Fact]
    public void MemoryRoundTrip_PreservesValuesAndWritesEnumNames()
    {
        MonitoringArchive original = CreateArchive();

        using var stream = new MemoryStream();

        ArchiveSerializer.WriteArchive(stream, original);

        string json = Encoding.UTF8.GetString(stream.ToArray());

        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;

            foreach (JsonElement reading
                     in root.GetProperty("readings").EnumerateArray())
            {
                JsonElement storageClass =
                    reading.GetProperty("storageClass");

                Assert.Equal(JsonValueKind.String, storageClass.ValueKind);
                Assert.Equal("Cold", storageClass.GetString());
            }

            foreach (JsonElement alert
                     in root.GetProperty("alerts").EnumerateArray())
            {
                JsonElement storageClass =
                    alert.GetProperty("storageClass");

                Assert.Equal(JsonValueKind.String, storageClass.ValueKind);
                Assert.Equal("Cold", storageClass.GetString());
            }
        }

        stream.Position = 0;

        MonitoringArchive restored =
            ArchiveSerializer.ReadArchive(stream);

        Assert.True(
            original.CreatedAtUtc.EqualsExact(restored.CreatedAtUtc));

        Assert.True(original.Readings.SequenceEqual(restored.Readings));
        Assert.True(original.Errors.SequenceEqual(restored.Errors));
        Assert.True(original.Alerts.SequenceEqual(restored.Alerts));
    }

    [Fact]
    public void StoredResults_WorkAfterSourceStreamIsDisposed()
    {
        string text =
            "S1|2026-09-22T08:00:00Z|Cold|4.0\n" +
            "S3|bad-date|Cold|3.5";

        ImportResult result;

        using (var stream = new MemoryStream(
                   Encoding.UTF8.GetBytes(text)))
        {
            result = ReadingImporter.ReadReadings(stream);
        }

        Reading[] readings = result.Readings.ToArray();
        ImportError[] errors = result.Errors.ToArray();

        Reading reading = Assert.Single(readings);
        ImportError error = Assert.Single(errors);

        Assert.Equal("S1", reading.SensorId);
        Assert.Equal(4.0m, reading.Temperature);
        Assert.Equal(2, error.LineNumber);
        Assert.Equal("S3|bad-date|Cold|3.5", error.RawLine);
    }

    [Fact]
    public void ReadReadings_StartsAtCurrentStreamPosition()
    {
        string prefix = "This line must not be read.\n";
        string validLine = "S1|2026-09-22T08:00:00Z|Cold|4.0";

        byte[] bytes = Encoding.UTF8.GetBytes(prefix + validLine);

        using var stream = new MemoryStream(bytes);

        stream.Position = Encoding.UTF8.GetByteCount(prefix);

        ImportResult result = ReadingImporter.ReadReadings(stream);

        Reading reading = Assert.Single(result.Readings);

        Assert.Equal("S1", reading.SensorId);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void MalformedJson_ThrowsJsonException()
    {
        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes("{ invalid json"));

        Assert.Throws<JsonException>(() =>
        {
            ArchiveSerializer.ReadArchive(stream);
        });
    }

    [Fact]
    public void NullArchive_ThrowsJsonException()
    {
        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes("null"));

        Assert.Throws<JsonException>(() =>
        {
            ArchiveSerializer.ReadArchive(stream);
        });
    }
}