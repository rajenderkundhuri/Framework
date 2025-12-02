using Framework.Domain.Common.Entities;

namespace Framework.Domain.Identity;

/// <summary>
/// User profile with preferences and settings
/// </summary>
public class UserProfile : AuditableEntity
{
    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private UserProfile() : base()
    {
    }

    /// <summary>
    /// Creates a new user profile
    /// </summary>
    public UserProfile(Guid id, Guid userId) : base(id)
    {
        UserId = userId;
    }

    /// <summary>
    /// Associated user ID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// User's preferred timezone (IANA timezone ID, e.g., "America/New_York")
    /// </summary>
    public string TimeZoneId { get; private set; } = "UTC";

    /// <summary>
    /// User's preferred date format (e.g., "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd")
    /// </summary>
    public string DateFormat { get; private set; } = "yyyy-MM-dd";

    /// <summary>
    /// User's preferred time format (e.g., "HH:mm:ss", "hh:mm:ss tt")
    /// </summary>
    public string TimeFormat { get; private set; } = "HH:mm:ss";

    /// <summary>
    /// User's preferred datetime format
    /// </summary>
    public string DateTimeFormat => $"{DateFormat} {TimeFormat}";

    /// <summary>
    /// User's preferred currency code (ISO 4217, e.g., "USD", "EUR", "GBP")
    /// </summary>
    public string CurrencyCode { get; private set; } = "USD";

    /// <summary>
    /// User's preferred language/locale (e.g., "en-US", "es-ES", "fr-FR")
    /// </summary>
    public string Locale { get; private set; } = "en-US";

    /// <summary>
    /// User's preferred number format locale
    /// </summary>
    public string NumberFormatLocale { get; private set; } = "en-US";

    /// <summary>
    /// Theme preference (light, dark, system)
    /// </summary>
    public ThemePreference Theme { get; private set; } = ThemePreference.System;

    /// <summary>
    /// Theme color preset name (e.g., "Default", "Purple", "Teal")
    /// </summary>
    public string ThemeColorName { get; private set; } = "Default";

    /// <summary>
    /// Email notification preferences
    /// </summary>
    public bool EmailNotificationsEnabled { get; private set; } = true;

    /// <summary>
    /// Push notification preferences
    /// </summary>
    public bool PushNotificationsEnabled { get; private set; } = true;

    /// <summary>
    /// Two-factor authentication preference
    /// </summary>
    public TwoFactorMethod PreferredTwoFactorMethod { get; private set; } = TwoFactorMethod.None;

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual ApplicationUser User { get; private set; } = null!;

    /// <summary>
    /// Update timezone preference
    /// </summary>
    public void SetTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("TimeZone ID cannot be empty", nameof(timeZoneId));

        // Validate timezone exists
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Try IANA format conversion for Windows
            if (!TryConvertIanaToWindows(timeZoneId, out _))
                throw new ArgumentException($"Invalid timezone: {timeZoneId}", nameof(timeZoneId));
        }

        TimeZoneId = timeZoneId;
    }

    /// <summary>
    /// Update date format preference
    /// </summary>
    public void SetDateFormat(string dateFormat)
    {
        if (string.IsNullOrWhiteSpace(dateFormat))
            throw new ArgumentException("Date format cannot be empty", nameof(dateFormat));

        DateFormat = dateFormat;
    }

    /// <summary>
    /// Update time format preference
    /// </summary>
    public void SetTimeFormat(string timeFormat)
    {
        if (string.IsNullOrWhiteSpace(timeFormat))
            throw new ArgumentException("Time format cannot be empty", nameof(timeFormat));

        TimeFormat = timeFormat;
    }

    /// <summary>
    /// Update currency preference
    /// </summary>
    public void SetCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code cannot be empty", nameof(currencyCode));

        if (currencyCode.Length != 3)
            throw new ArgumentException("Currency code must be 3 characters (ISO 4217)", nameof(currencyCode));

        CurrencyCode = currencyCode.ToUpperInvariant();
    }

    /// <summary>
    /// Update locale preference
    /// </summary>
    public void SetLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            throw new ArgumentException("Locale cannot be empty", nameof(locale));

        Locale = locale;
    }

    /// <summary>
    /// Update number format locale
    /// </summary>
    public void SetNumberFormatLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            throw new ArgumentException("Number format locale cannot be empty", nameof(locale));

        NumberFormatLocale = locale;
    }

    /// <summary>
    /// Update theme preference
    /// </summary>
    public void SetTheme(ThemePreference theme)
    {
        Theme = theme;
    }

    /// <summary>
    /// Update theme color preset name
    /// </summary>
    public void SetThemeColorName(string colorName)
    {
        if (string.IsNullOrWhiteSpace(colorName))
            colorName = "Default";

        ThemeColorName = colorName;
    }

    /// <summary>
    /// Update both theme preference and color name
    /// </summary>
    public void SetThemeSettings(ThemePreference theme, string colorName)
    {
        Theme = theme;
        SetThemeColorName(colorName);
    }

    /// <summary>
    /// Update email notifications preference
    /// </summary>
    public void SetEmailNotifications(bool enabled)
    {
        EmailNotificationsEnabled = enabled;
    }

    /// <summary>
    /// Update push notifications preference
    /// </summary>
    public void SetPushNotifications(bool enabled)
    {
        PushNotificationsEnabled = enabled;
    }

    /// <summary>
    /// Update two-factor method preference
    /// </summary>
    public void SetPreferredTwoFactorMethod(TwoFactorMethod method)
    {
        PreferredTwoFactorMethod = method;
    }

    /// <summary>
    /// Get the user's timezone info
    /// </summary>
    public TimeZoneInfo GetTimeZoneInfo()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            if (TryConvertIanaToWindows(TimeZoneId, out var windowsId))
                return TimeZoneInfo.FindSystemTimeZoneById(windowsId);

            return TimeZoneInfo.Utc;
        }
    }

    /// <summary>
    /// Convert a UTC datetime to the user's local time
    /// </summary>
    public DateTime ConvertToUserTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, GetTimeZoneInfo());
    }

    /// <summary>
    /// Convert a user's local time to UTC
    /// </summary>
    public DateTime ConvertToUtc(DateTime userDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(userDateTime, GetTimeZoneInfo());
    }

    /// <summary>
    /// Format a datetime using user's preferences
    /// </summary>
    public string FormatDateTime(DateTime dateTime)
    {
        var userTime = dateTime.Kind == DateTimeKind.Utc
            ? ConvertToUserTime(dateTime)
            : dateTime;

        return userTime.ToString(DateTimeFormat);
    }

    /// <summary>
    /// Format a date using user's preferences
    /// </summary>
    public string FormatDate(DateTime date)
    {
        var userTime = date.Kind == DateTimeKind.Utc
            ? ConvertToUserTime(date)
            : date;

        return userTime.ToString(DateFormat);
    }

    private static bool TryConvertIanaToWindows(string ianaId, out string windowsId)
    {
        // Common IANA to Windows timezone mappings
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["America/New_York"] = "Eastern Standard Time",
            ["America/Chicago"] = "Central Standard Time",
            ["America/Denver"] = "Mountain Standard Time",
            ["America/Los_Angeles"] = "Pacific Standard Time",
            ["Europe/London"] = "GMT Standard Time",
            ["Europe/Paris"] = "Romance Standard Time",
            ["Europe/Berlin"] = "W. Europe Standard Time",
            ["Asia/Tokyo"] = "Tokyo Standard Time",
            ["Asia/Shanghai"] = "China Standard Time",
            ["Asia/Kolkata"] = "India Standard Time",
            ["Australia/Sydney"] = "AUS Eastern Standard Time",
            ["UTC"] = "UTC"
        };

        return mappings.TryGetValue(ianaId, out windowsId!);
    }
}

/// <summary>
/// Theme preference options
/// </summary>
public enum ThemePreference
{
    /// <summary>
    /// Follow system preference
    /// </summary>
    System = 0,

    /// <summary>
    /// Light theme
    /// </summary>
    Light = 1,

    /// <summary>
    /// Dark theme
    /// </summary>
    Dark = 2
}

/// <summary>
/// Two-factor authentication methods
/// </summary>
public enum TwoFactorMethod
{
    /// <summary>
    /// No two-factor authentication
    /// </summary>
    None = 0,

    /// <summary>
    /// Email-based OTP
    /// </summary>
    Email = 1,

    /// <summary>
    /// SMS-based OTP
    /// </summary>
    Sms = 2,

    /// <summary>
    /// Authenticator app (TOTP)
    /// </summary>
    Authenticator = 3
}
