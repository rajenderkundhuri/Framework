using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Framework.Admin.Services;

public record ThemePreset(string Name, string PrimaryColor, string SecondaryColor, string AppbarColor);

public interface IThemeService
{
    bool IsDarkMode { get; }
    string CurrentThemeName { get; }
    MudTheme CurrentTheme { get; }
    IReadOnlyList<ThemePreset> AvailableThemes { get; }
    event Action? OnThemeChanged;
    Task InitializeAsync();
    Task ToggleDarkModeAsync();
    Task SetThemeAsync(string themeName);
    void SetDarkMode(bool isDark);
    Task LoadFromServerAsync();
}

public class ThemeService : IThemeService
{
    private const string ThemeKey = "theme_dark_mode";
    private const string ThemeNameKey = "theme_name";
    private readonly ILocalStorageService _localStorage;
    private readonly IProfileApiService _profileApiService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private bool _isDarkMode;
    private string _currentThemeName = "Default";
    private MudTheme _currentTheme = null!;
    private bool _initialized;
    private bool _isSyncing;

    private static readonly List<ThemePreset> _themes = new()
    {
        new("Default", "#1976d2", "#424242", "#1976d2"),
        new("Purple", "#7c4dff", "#536dfe", "#7c4dff"),
        new("Teal", "#009688", "#00bcd4", "#009688"),
        new("Orange", "#ff5722", "#ff9800", "#ff5722"),
        new("Green", "#4caf50", "#8bc34a", "#4caf50"),
        new("Red", "#f44336", "#e91e63", "#f44336"),
        new("Indigo", "#3f51b5", "#673ab7", "#3f51b5"),
        new("Cyan", "#00bcd4", "#03a9f4", "#00bcd4"),
        new("Pink", "#e91e63", "#f48fb1", "#e91e63"),
        new("Amber", "#ffc107", "#ffb300", "#ff8f00")
    };

    public ThemeService(
        ILocalStorageService localStorage,
        IProfileApiService profileApiService,
        AuthenticationStateProvider authStateProvider)
    {
        _localStorage = localStorage;
        _profileApiService = profileApiService;
        _authStateProvider = authStateProvider;
        _currentTheme = BuildTheme(_themes[0]);
    }

    public bool IsDarkMode => _isDarkMode;
    public string CurrentThemeName => _currentThemeName;
    public MudTheme CurrentTheme => _currentTheme;
    public IReadOnlyList<ThemePreset> AvailableThemes => _themes;

    public event Action? OnThemeChanged;

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            _isDarkMode = await _localStorage.GetItemAsync<bool>(ThemeKey);
            var savedThemeName = await _localStorage.GetItemAsync<string>(ThemeNameKey);
            if (!string.IsNullOrEmpty(savedThemeName))
            {
                var preset = _themes.FirstOrDefault(t => t.Name == savedThemeName);
                if (preset != null)
                {
                    _currentThemeName = preset.Name;
                    _currentTheme = BuildTheme(preset);
                }
            }
        }
        catch
        {
            _isDarkMode = false;
            _currentThemeName = "Default";
            _currentTheme = BuildTheme(_themes[0]);
        }

        _initialized = true;
        OnThemeChanged?.Invoke();
    }

    public async Task ToggleDarkModeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await _localStorage.SetItemAsync(ThemeKey, _isDarkMode);
        OnThemeChanged?.Invoke();
        await SaveToServerAsync();
    }

    public async Task SetThemeAsync(string themeName)
    {
        var preset = _themes.FirstOrDefault(t => t.Name == themeName);
        if (preset == null) return;

        _currentThemeName = preset.Name;
        _currentTheme = BuildTheme(preset);
        await _localStorage.SetItemAsync(ThemeNameKey, themeName);
        OnThemeChanged?.Invoke();
        await SaveToServerAsync();
    }

    public async Task LoadFromServerAsync()
    {
        if (_isSyncing) return;

        try
        {
            _isSyncing = true;
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated != true) return;

            var result = await _profileApiService.GetThemeSettingsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                _isDarkMode = result.Value.IsDarkMode;
                var preset = _themes.FirstOrDefault(t => t.Name == result.Value.ThemeColorName) ?? _themes[0];
                _currentThemeName = preset.Name;
                _currentTheme = BuildTheme(preset);

                // Also save to local storage for faster subsequent loads
                await _localStorage.SetItemAsync(ThemeKey, _isDarkMode);
                await _localStorage.SetItemAsync(ThemeNameKey, _currentThemeName);

                OnThemeChanged?.Invoke();
            }
        }
        catch
        {
            // Ignore errors when loading from server - fall back to local storage
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private async Task SaveToServerAsync()
    {
        if (_isSyncing) return;

        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated != true) return;

            await _profileApiService.UpdateThemeSettingsAsync(_isDarkMode, _currentThemeName);
        }
        catch
        {
            // Ignore errors when saving to server
        }
    }

    public void SetDarkMode(bool isDark)
    {
        if (_isDarkMode != isDark)
        {
            _isDarkMode = isDark;
            OnThemeChanged?.Invoke();
        }
    }

    private static MudTheme BuildTheme(ThemePreset preset)
    {
        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = preset.PrimaryColor,
                Secondary = preset.SecondaryColor,
                AppbarBackground = preset.AppbarColor
            },
            PaletteDark = new PaletteDark
            {
                Primary = LightenColor(preset.PrimaryColor),
                Secondary = LightenColor(preset.SecondaryColor),
                AppbarBackground = "#1e1e1e"
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = new[] { "Roboto", "Helvetica", "Arial", "sans-serif" }
                }
            }
        };
    }

    private static string LightenColor(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#"))
            return hexColor;

        try
        {
            var hex = hexColor.TrimStart('#');
            var r = Math.Min(255, Convert.ToInt32(hex.Substring(0, 2), 16) + 60);
            var g = Math.Min(255, Convert.ToInt32(hex.Substring(2, 2), 16) + 60);
            var b = Math.Min(255, Convert.ToInt32(hex.Substring(4, 2), 16) + 60);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return hexColor;
        }
    }
}
