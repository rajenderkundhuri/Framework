using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using Framework.Admin;
using Framework.Admin.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configuration
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

// Global JSON Serializer Options for API communication
builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
});

// HTTP Client Factory for typed clients with authorization
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthorizationMessageHandler>();

// Plain HTTP Client for auth endpoints (login, register, etc.)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// MudBlazor
builder.Services.AddMudServices();

// Local Storage
builder.Services.AddBlazoredLocalStorage();

// Authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();

// API Services
builder.Services.AddScoped<IUserApiService, UserApiService>();
builder.Services.AddScoped<IRoleApiService, RoleApiService>();
builder.Services.AddScoped<ITenantApiService, TenantApiService>();
builder.Services.AddScoped<IResourceApiService, ResourceApiService>();
builder.Services.AddScoped<ILanguageApiService, LanguageApiService>();
builder.Services.AddScoped<IProfileApiService, ProfileApiService>();
builder.Services.AddScoped<IAuditLogApiService, AuditLogApiService>();
builder.Services.AddScoped<ISystemApiService, SystemApiService>();
builder.Services.AddScoped<IThemeService, ThemeService>();

// New Module Services
builder.Services.AddScoped<IEmailTemplateApiService, EmailTemplateApiService>();
builder.Services.AddScoped<INotificationApiService, NotificationApiService>();
builder.Services.AddScoped<IApiKeyApiService, ApiKeyApiService>();
builder.Services.AddScoped<ITwoFactorApiService, TwoFactorApiService>();
builder.Services.AddScoped<IBackgroundJobApiService, BackgroundJobApiService>();

await builder.Build().RunAsync();
