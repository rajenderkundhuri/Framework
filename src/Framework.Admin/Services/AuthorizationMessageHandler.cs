using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace Framework.Admin.Services;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;

    private const string TokenKey = "authToken";

    public AuthorizationMessageHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Resolve ILocalStorageService at request time to ensure proper scope
        var localStorage = _serviceProvider.GetRequiredService<ILocalStorageService>();
        var token = await localStorage.GetItemAsync<string>(TokenKey);

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
