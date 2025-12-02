namespace Framework.Application.BackgroundJobs;

/// <summary>
/// Helper class for common CRON expressions
/// </summary>
public static class CronExpressions
{
    /// <summary>
    /// Runs every minute
    /// </summary>
    public const string EveryMinute = "* * * * *";

    /// <summary>
    /// Runs every 5 minutes
    /// </summary>
    public const string Every5Minutes = "*/5 * * * *";

    /// <summary>
    /// Runs every 10 minutes
    /// </summary>
    public const string Every10Minutes = "*/10 * * * *";

    /// <summary>
    /// Runs every 15 minutes
    /// </summary>
    public const string Every15Minutes = "*/15 * * * *";

    /// <summary>
    /// Runs every 30 minutes
    /// </summary>
    public const string Every30Minutes = "*/30 * * * *";

    /// <summary>
    /// Runs every hour at minute 0
    /// </summary>
    public const string Hourly = "0 * * * *";

    /// <summary>
    /// Runs every day at midnight
    /// </summary>
    public const string Daily = "0 0 * * *";

    /// <summary>
    /// Runs every day at 6 AM
    /// </summary>
    public const string DailyAt6AM = "0 6 * * *";

    /// <summary>
    /// Runs every day at noon
    /// </summary>
    public const string DailyAtNoon = "0 12 * * *";

    /// <summary>
    /// Runs every day at 6 PM
    /// </summary>
    public const string DailyAt6PM = "0 18 * * *";

    /// <summary>
    /// Runs every Monday at midnight
    /// </summary>
    public const string Weekly = "0 0 * * 1";

    /// <summary>
    /// Runs on the first day of every month at midnight
    /// </summary>
    public const string Monthly = "0 0 1 * *";

    /// <summary>
    /// Runs on January 1st at midnight
    /// </summary>
    public const string Yearly = "0 0 1 1 *";

    /// <summary>
    /// Runs Monday through Friday at 9 AM
    /// </summary>
    public const string Weekdays9AM = "0 9 * * 1-5";

    /// <summary>
    /// Runs Monday through Friday at 5 PM
    /// </summary>
    public const string Weekdays5PM = "0 17 * * 1-5";

    /// <summary>
    /// Creates a CRON expression for a specific minute of every hour
    /// </summary>
    /// <param name="minute">Minute (0-59)</param>
    public static string HourlyAt(int minute)
    {
        ValidateMinute(minute);
        return $"{minute} * * * *";
    }

    /// <summary>
    /// Creates a CRON expression for a specific time every day
    /// </summary>
    /// <param name="hour">Hour (0-23)</param>
    /// <param name="minute">Minute (0-59)</param>
    public static string DailyAt(int hour, int minute = 0)
    {
        ValidateHour(hour);
        ValidateMinute(minute);
        return $"{minute} {hour} * * *";
    }

    /// <summary>
    /// Creates a CRON expression for a specific time on specific days of the week
    /// </summary>
    /// <param name="hour">Hour (0-23)</param>
    /// <param name="minute">Minute (0-59)</param>
    /// <param name="daysOfWeek">Days of week (0=Sunday, 6=Saturday)</param>
    public static string WeeklyAt(int hour, int minute, params DayOfWeek[] daysOfWeek)
    {
        ValidateHour(hour);
        ValidateMinute(minute);

        if (daysOfWeek == null || daysOfWeek.Length == 0)
            throw new ArgumentException("At least one day of week must be specified", nameof(daysOfWeek));

        var days = string.Join(",", daysOfWeek.Select(d => (int)d));
        return $"{minute} {hour} * * {days}";
    }

    /// <summary>
    /// Creates a CRON expression for a specific day and time each month
    /// </summary>
    /// <param name="dayOfMonth">Day of month (1-31)</param>
    /// <param name="hour">Hour (0-23)</param>
    /// <param name="minute">Minute (0-59)</param>
    public static string MonthlyAt(int dayOfMonth, int hour = 0, int minute = 0)
    {
        ValidateDayOfMonth(dayOfMonth);
        ValidateHour(hour);
        ValidateMinute(minute);
        return $"{minute} {hour} {dayOfMonth} * *";
    }

    /// <summary>
    /// Creates a CRON expression that runs every N minutes
    /// </summary>
    /// <param name="interval">Interval in minutes</param>
    public static string EveryNMinutes(int interval)
    {
        if (interval < 1 || interval > 59)
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be between 1 and 59");

        return $"*/{interval} * * * *";
    }

    /// <summary>
    /// Creates a CRON expression that runs every N hours
    /// </summary>
    /// <param name="interval">Interval in hours</param>
    public static string EveryNHours(int interval)
    {
        if (interval < 1 || interval > 23)
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be between 1 and 23");

        return $"0 */{interval} * * *";
    }

    /// <summary>
    /// Validates a CRON expression format (basic validation)
    /// </summary>
    /// <param name="cronExpression">CRON expression to validate</param>
    /// <returns>True if valid format</returns>
    public static bool IsValid(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return false;

        var parts = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Standard CRON has 5 parts: minute hour day month dayOfWeek
        return parts.Length == 5;
    }

    /// <summary>
    /// Describes a CRON expression in human-readable format
    /// </summary>
    /// <param name="cronExpression">CRON expression</param>
    /// <returns>Human-readable description</returns>
    public static string Describe(string cronExpression)
    {
        if (!IsValid(cronExpression))
            return "Invalid CRON expression";

        return cronExpression switch
        {
            EveryMinute => "Every minute",
            Every5Minutes => "Every 5 minutes",
            Every10Minutes => "Every 10 minutes",
            Every15Minutes => "Every 15 minutes",
            Every30Minutes => "Every 30 minutes",
            Hourly => "Every hour",
            Daily => "Every day at midnight",
            DailyAtNoon => "Every day at noon",
            Weekly => "Every Monday at midnight",
            Monthly => "First day of every month at midnight",
            Yearly => "January 1st at midnight",
            _ => cronExpression
        };
    }

    private static void ValidateMinute(int minute)
    {
        if (minute < 0 || minute > 59)
            throw new ArgumentOutOfRangeException(nameof(minute), "Minute must be between 0 and 59");
    }

    private static void ValidateHour(int hour)
    {
        if (hour < 0 || hour > 23)
            throw new ArgumentOutOfRangeException(nameof(hour), "Hour must be between 0 and 23");
    }

    private static void ValidateDayOfMonth(int day)
    {
        if (day < 1 || day > 31)
            throw new ArgumentOutOfRangeException(nameof(day), "Day of month must be between 1 and 31");
    }
}
