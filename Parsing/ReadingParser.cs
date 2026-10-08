using System.Globalization;
using ColdChainMonitor.Models;

namespace ColdChainMonitor.Parsing;

public static class ReadingParser
{
    public static bool TryParseReading(
        string rawLine,
        out Reading? reading,
        out string error)
    {
        reading = null;
        error = string.Empty;

        string[] fields = rawLine.Split('|');

        if (fields.Length != 4)
        {
            error = "Expected exactly 4 pipe-separated fields.";
            return false;
        }

        string sensorId = fields[0].Trim();
        string timestampText = fields[1].Trim();
        string storageClassText = fields[2].Trim();
        string temperatureText = fields[3].Trim();

        if (string.IsNullOrWhiteSpace(sensorId))
        {
            error = "Sensor ID must not be empty.";
            return false;
        }

        bool timestampIsValid = DateTimeOffset.TryParseExact(
            timestampText,
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal
                | DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset timestamp);

        if (!timestampIsValid)
        {
            error = "Timestamp must use yyyy-MM-ddTHH:mm:ssZ format.";
            return false;
        }

        if (!Enum.TryParse<StorageClass>(
                storageClassText,
                ignoreCase: true,
                out StorageClass storageClass)
            || !Enum.IsDefined(typeof(StorageClass), storageClass))
        {
            error = "Storage class must be a defined Cold or Frozen value.";
            return false;
        }

        bool temperatureIsValid = decimal.TryParse(
            temperatureText,
            NumberStyles.AllowLeadingSign
                | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out decimal temperature);

        if (!temperatureIsValid)
        {
            error = "Temperature must be a decimal number with a dot.";
            return false;
        }

        reading = new Reading(
            sensorId,
            timestamp,
            storageClass,
            temperature);

        return true;
    }
}