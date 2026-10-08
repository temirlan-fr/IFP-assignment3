using ColdChainMonitor.Analysis;
using ColdChainMonitor.Models;
using Xunit;

namespace ColdChainMonitor.Tests;

public sealed class AnalysisTests
{
    private static Reading MakeReading(
        string sensorId,
        int minute,
        decimal temperature,
        StorageClass storageClass = StorageClass.Cold)
    {
        return new Reading(
            sensorId,
            new DateTimeOffset(
                2026, 9, 22, 8, minute, 0, TimeSpan.Zero),
            storageClass,
            temperature);
    }

    [Fact]
    public void SafeBoundaryTemperatures_DoNotCreateAlerts()
    {
        Reading[] readings =
        {
            MakeReading("C1", 0, 2.0m),
            MakeReading("C2", 0, 8.0m),
            MakeReading("F1", 0, -22.0m, StorageClass.Frozen),
            MakeReading("F2", 0, -15.0m, StorageClass.Frozen)
        };

        var alerts = ReadingAnalyzer.Analyze(readings);

        Assert.Empty(alerts);
    }

    [Fact]
    public void AdjacentComparisons_DoNotCrossSensorBoundaries()
    {
        Reading[] readings =
        {
            MakeReading("S1", 0, 4.0m),
            MakeReading("S2", 1, -18.0m, StorageClass.Frozen)
        };

        var alerts = ReadingAnalyzer.Analyze(readings);

        Assert.Empty(alerts);
    }

    [Fact]
    public void UnsortedInput_ProducesSameAlertsAsSortedInput()
    {
        Reading first = MakeReading("S1", 0, 4.0m);
        Reading second = MakeReading("S1", 5, 8.7m);
        Reading third = MakeReading("S1", 10, 5.2m);

        Reading[] sorted = { first, second, third };
        Reading[] unsorted = { third, first, second };

        var expected = ReadingAnalyzer.Analyze(sorted);
        var actual = ReadingAnalyzer.Analyze(unsorted);

        Assert.Single(expected);
        Assert.True(expected.SequenceEqual(actual));

        Alert alert = Assert.Single(actual);

        Assert.Equal(second.Timestamp, alert.Timestamp);
        Assert.True(alert.IsOutsideRange);
        Assert.True(alert.IsAbruptChange);
    }

    [Fact]
    public void ChangeOfExactlyFourDegrees_IsNotAbrupt()
    {
        Reading[] readings =
        {
            MakeReading("S1", 0, 2.0m),
            MakeReading("S1", 5, 6.0m)
        };

        var alerts = ReadingAnalyzer.Analyze(readings);

        Assert.Empty(alerts);
    }

    [Fact]
    public void FirstReading_CanBeOutsideRangeButNotAbrupt()
    {
        Reading[] readings =
        {
            MakeReading("S1", 0, 9.0m)
        };

        var alerts = ReadingAnalyzer.Analyze(readings);

        Alert alert = Assert.Single(alerts);

        Assert.True(alert.IsOutsideRange);
        Assert.False(alert.IsAbruptChange);
    }

    [Fact]
    public void BothReasons_AreStoredInOneAlert()
    {
        Reading[] readings =
        {
            MakeReading("S1", 0, 4.0m),
            MakeReading("S1", 5, 8.7m)
        };

        var alerts = ReadingAnalyzer.Analyze(readings);

        Alert alert = Assert.Single(alerts);

        Assert.Equal(8.7m, alert.Temperature);
        Assert.True(alert.IsOutsideRange);
        Assert.True(alert.IsAbruptChange);
    }

    [Fact]
    public void EqualTimestamps_PreserveOriginalInputOrder()
    {
        Reading[] readings =
        {
            MakeReading("S1", 0, 4.0m),
            MakeReading("S1", 0, 9.0m),
            MakeReading("S1", 0, 3.0m)
        };

        var alerts = ReadingAnalyzer.Analyze(readings);

        Assert.Equal(2, alerts.Count);

        Assert.Equal(9.0m, alerts[0].Temperature);
        Assert.True(alerts[0].IsOutsideRange);
        Assert.True(alerts[0].IsAbruptChange);

        Assert.Equal(3.0m, alerts[1].Temperature);
        Assert.False(alerts[1].IsOutsideRange);
        Assert.True(alerts[1].IsAbruptChange);
    }

    [Fact]
    public void Alerts_AreSortedAndUseFirstAcceptedSensorSpelling()
    {
        Reading[] readings =
        {
            MakeReading("b", 0, 4.0m),
            MakeReading("B", 10, 9.0m),
            MakeReading("a", 0, 4.0m),
            MakeReading("A", 10, 9.0m),
            MakeReading("A", 5, 8.7m)
        };

        var alerts = ReadingAnalyzer.Analyze(readings);

        Assert.Equal(3, alerts.Count);

        Assert.Equal("a", alerts[0].SensorId);
        Assert.Equal(5, alerts[0].Timestamp.Minute);

        Assert.Equal("a", alerts[1].SensorId);
        Assert.Equal(10, alerts[1].Timestamp.Minute);

        Assert.Equal("b", alerts[2].SensorId);
        Assert.Equal(10, alerts[2].Timestamp.Minute);

        Assert.Equal("B", readings[1].SensorId);
        Assert.Equal("A", readings[3].SensorId);
    }
}