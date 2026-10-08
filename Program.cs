using ColdChainMonitor.Models;

var reading = new Reading(
    SensorId: "S1",
    Timestamp: new DateTimeOffset(
        2026, 9, 22, 8, 10, 0, TimeSpan.Zero),
    StorageClass: StorageClass.Cold,
    Temperature: 5.2m);

Console.WriteLine("Cold Chain Monitor");
Console.WriteLine($"Sensor: {reading.SensorId}");
Console.WriteLine($"Storage class: {reading.StorageClass}");
Console.WriteLine(FormattableString.Invariant(
    $"Temperature: {reading.Temperature} C"));
Console.WriteLine($"UTC offset: {reading.Timestamp.Offset}");