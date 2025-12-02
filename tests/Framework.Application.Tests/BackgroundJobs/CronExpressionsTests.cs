using Framework.Application.BackgroundJobs;
using Shouldly;

namespace Framework.Application.Tests.BackgroundJobs;

public class CronExpressionsTests
{
    [Fact]
    public void Constants_ShouldHaveCorrectValues()
    {
        CronExpressions.EveryMinute.ShouldBe("* * * * *");
        CronExpressions.Every5Minutes.ShouldBe("*/5 * * * *");
        CronExpressions.Every10Minutes.ShouldBe("*/10 * * * *");
        CronExpressions.Every15Minutes.ShouldBe("*/15 * * * *");
        CronExpressions.Every30Minutes.ShouldBe("*/30 * * * *");
        CronExpressions.Hourly.ShouldBe("0 * * * *");
        CronExpressions.Daily.ShouldBe("0 0 * * *");
        CronExpressions.DailyAt6AM.ShouldBe("0 6 * * *");
        CronExpressions.DailyAtNoon.ShouldBe("0 12 * * *");
        CronExpressions.DailyAt6PM.ShouldBe("0 18 * * *");
        CronExpressions.Weekly.ShouldBe("0 0 * * 1");
        CronExpressions.Monthly.ShouldBe("0 0 1 * *");
        CronExpressions.Yearly.ShouldBe("0 0 1 1 *");
        CronExpressions.Weekdays9AM.ShouldBe("0 9 * * 1-5");
        CronExpressions.Weekdays5PM.ShouldBe("0 17 * * 1-5");
    }

    [Theory]
    [InlineData(0, "0 * * * *")]
    [InlineData(15, "15 * * * *")]
    [InlineData(30, "30 * * * *")]
    [InlineData(59, "59 * * * *")]
    public void HourlyAt_ShouldGenerateCorrectExpression(int minute, string expected)
    {
        CronExpressions.HourlyAt(minute).ShouldBe(expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60)]
    public void HourlyAt_WithInvalidMinute_ShouldThrow(int minute)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CronExpressions.HourlyAt(minute));
    }

    [Theory]
    [InlineData(0, 0, "0 0 * * *")]
    [InlineData(6, 0, "0 6 * * *")]
    [InlineData(12, 30, "30 12 * * *")]
    [InlineData(23, 59, "59 23 * * *")]
    public void DailyAt_ShouldGenerateCorrectExpression(int hour, int minute, string expected)
    {
        CronExpressions.DailyAt(hour, minute).ShouldBe(expected);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(24, 0)]
    public void DailyAt_WithInvalidHour_ShouldThrow(int hour, int minute)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CronExpressions.DailyAt(hour, minute));
    }

    [Theory]
    [InlineData(9, -1)]
    [InlineData(9, 60)]
    public void DailyAt_WithInvalidMinute_ShouldThrow(int hour, int minute)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CronExpressions.DailyAt(hour, minute));
    }

    [Fact]
    public void WeeklyAt_WithSingleDay_ShouldGenerateCorrectExpression()
    {
        // Monday at 9:00 AM
        var result = CronExpressions.WeeklyAt(9, 0, DayOfWeek.Monday);
        result.ShouldBe("0 9 * * 1");
    }

    [Fact]
    public void WeeklyAt_WithMultipleDays_ShouldGenerateCorrectExpression()
    {
        // Weekdays (Mon-Fri) at 9:00 AM
        var result = CronExpressions.WeeklyAt(9, 0,
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday);
        result.ShouldBe("0 9 * * 1,2,3,4,5");
    }

    [Fact]
    public void WeeklyAt_WithNoDays_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => CronExpressions.WeeklyAt(9, 0));
    }

    [Theory]
    [InlineData(1, 0, 0, "0 0 1 * *")]
    [InlineData(15, 12, 0, "0 12 15 * *")]
    [InlineData(28, 6, 30, "30 6 28 * *")]
    public void MonthlyAt_ShouldGenerateCorrectExpression(int day, int hour, int minute, string expected)
    {
        CronExpressions.MonthlyAt(day, hour, minute).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void MonthlyAt_WithInvalidDay_ShouldThrow(int day)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CronExpressions.MonthlyAt(day));
    }

    [Theory]
    [InlineData(1, "*/1 * * * *")]
    [InlineData(5, "*/5 * * * *")]
    [InlineData(15, "*/15 * * * *")]
    [InlineData(30, "*/30 * * * *")]
    public void EveryNMinutes_ShouldGenerateCorrectExpression(int interval, string expected)
    {
        CronExpressions.EveryNMinutes(interval).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    public void EveryNMinutes_WithInvalidInterval_ShouldThrow(int interval)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CronExpressions.EveryNMinutes(interval));
    }

    [Theory]
    [InlineData(1, "0 */1 * * *")]
    [InlineData(2, "0 */2 * * *")]
    [InlineData(6, "0 */6 * * *")]
    [InlineData(12, "0 */12 * * *")]
    public void EveryNHours_ShouldGenerateCorrectExpression(int interval, string expected)
    {
        CronExpressions.EveryNHours(interval).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(24)]
    public void EveryNHours_WithInvalidInterval_ShouldThrow(int interval)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CronExpressions.EveryNHours(interval));
    }

    [Theory]
    [InlineData("* * * * *", true)]
    [InlineData("0 0 * * *", true)]
    [InlineData("*/5 * * * *", true)]
    [InlineData("0 9 * * 1-5", true)]
    [InlineData("0 0 1 1 *", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("* * * *", false)] // Only 4 parts
    [InlineData("* * * * * *", false)] // 6 parts
    public void IsValid_ShouldReturnCorrectly(string? expression, bool expected)
    {
        CronExpressions.IsValid(expression!).ShouldBe(expected);
    }

    [Theory]
    [InlineData("* * * * *", "Every minute")]
    [InlineData("*/5 * * * *", "Every 5 minutes")]
    [InlineData("*/10 * * * *", "Every 10 minutes")]
    [InlineData("*/15 * * * *", "Every 15 minutes")]
    [InlineData("*/30 * * * *", "Every 30 minutes")]
    [InlineData("0 * * * *", "Every hour")]
    [InlineData("0 0 * * *", "Every day at midnight")]
    [InlineData("0 12 * * *", "Every day at noon")]
    [InlineData("0 0 * * 1", "Every Monday at midnight")]
    [InlineData("0 0 1 * *", "First day of every month at midnight")]
    [InlineData("0 0 1 1 *", "January 1st at midnight")]
    public void Describe_ShouldReturnHumanReadable(string expression, string expected)
    {
        CronExpressions.Describe(expression).ShouldBe(expected);
    }

    [Fact]
    public void Describe_WithCustomExpression_ShouldReturnExpression()
    {
        var expression = "0 9 * * 1-5";
        CronExpressions.Describe(expression).ShouldBe(expression);
    }

    [Fact]
    public void Describe_WithInvalidExpression_ShouldReturnErrorMessage()
    {
        CronExpressions.Describe("invalid").ShouldBe("Invalid CRON expression");
    }
}
