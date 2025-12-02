using System.Net.Http.Json;
using System.Text.Json;
using Framework.Application.Common.Models;
using Framework.Application.Identity;

namespace Framework.Admin.Services;

public interface ITwoFactorApiService
{
    Task<Result<TwoFactorStatusResponse>> GetStatusAsync();
    Task<Result<TwoFactorSetupResponse>> BeginSetupAsync();
    Task<Result<TwoFactorConfirmResponse>> ConfirmSetupAsync(string verificationCode);
    Task<Result> DisableAsync(string verificationCode);
    Task<Result<RecoveryCodesResponse>> RegenerateRecoveryCodesAsync();
    Task<Result<int>> GetRecoveryCodeCountAsync();
    Task<Result> ResetAuthenticatorAsync();
}

public class TwoFactorApiService : ITwoFactorApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public TwoFactorApiService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClientFactory.CreateClient("API");
        _jsonOptions = jsonOptions;
    }

    public async Task<Result<TwoFactorStatusResponse>> GetStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<TwoFactorStatusResponse>("api/twofactor/status", _jsonOptions);
            return response != null
                ? Result<TwoFactorStatusResponse>.Success(response)
                : Result<TwoFactorStatusResponse>.Failure("Failed to get 2FA status", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<TwoFactorStatusResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<TwoFactorSetupResponse>> BeginSetupAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/twofactor/setup", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TwoFactorSetupResponse>(_jsonOptions);
                return result != null
                    ? Result<TwoFactorSetupResponse>.Success(result)
                    : Result<TwoFactorSetupResponse>.Failure("Failed to setup 2FA", "API_ERROR");
            }
            var error = await response.Content.ReadAsStringAsync();
            return Result<TwoFactorSetupResponse>.Failure(error, "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<TwoFactorSetupResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<TwoFactorConfirmResponse>> ConfirmSetupAsync(string verificationCode)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/twofactor/setup/confirm", new { VerificationCode = verificationCode }, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TwoFactorConfirmResponse>(_jsonOptions);
                return result != null
                    ? Result<TwoFactorConfirmResponse>.Success(result)
                    : Result<TwoFactorConfirmResponse>.Failure("Failed to confirm 2FA", "API_ERROR");
            }
            var error = await response.Content.ReadAsStringAsync();
            return Result<TwoFactorConfirmResponse>.Failure(error, "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<TwoFactorConfirmResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> DisableAsync(string verificationCode)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/twofactor/disable", new { VerificationCode = verificationCode }, _jsonOptions);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<RecoveryCodesResponse>> RegenerateRecoveryCodesAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/twofactor/recovery-codes/regenerate", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RecoveryCodesResponse>(_jsonOptions);
                return result != null
                    ? Result<RecoveryCodesResponse>.Success(result)
                    : Result<RecoveryCodesResponse>.Failure("Failed to regenerate codes", "API_ERROR");
            }
            var error = await response.Content.ReadAsStringAsync();
            return Result<RecoveryCodesResponse>.Failure(error, "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<RecoveryCodesResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<int>> GetRecoveryCodeCountAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<int>("api/twofactor/recovery-codes/count", _jsonOptions);
            return Result<int>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> ResetAuthenticatorAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/twofactor/reset", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }
}
