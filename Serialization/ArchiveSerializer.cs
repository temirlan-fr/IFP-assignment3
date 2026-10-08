using System.Text.Json;
using System.Text.Json.Serialization;
using ColdChainMonitor.Models;

namespace ColdChainMonitor.Serialization;

public static class ArchiveSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(allowIntegerValues: false)
        }
    };

    public static void WriteArchive(
        Stream output,
        MonitoringArchive archive)
    {
        JsonSerializer.Serialize(output, archive, Options);
    }

    public static MonitoringArchive ReadArchive(Stream input)
    {
        MonitoringArchive? archive =
            JsonSerializer.Deserialize<MonitoringArchive>(
                input,
                Options);

        if (archive is null)
        {
            throw new JsonException("Archive must not be null.");
        }

        return archive;
    }
}