using Blazored.LocalStorage;
using CFW.AppHost.Blazor.ApiClient;
using CFW.Core.Utils;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace CFW.AppHost.Blazor.Authentications;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly IApiClient _apiClient;
    public const string AccessTokenKey = "access_token";
    private const string ExpiresAtKey = "access_expires_at"; // ISO 8601 string
    private const string RefreshTokenKey = "refresh_token";


    public CustomAuthStateProvider(ILocalStorageService localStorage, IApiClient apiClient)
    {
        _localStorage = localStorage;
        _apiClient = apiClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Retrieve the JWT token from local storage
        var token = await _localStorage.GetItemAsync<string>(AccessTokenKey);
        var expIso = await _localStorage.GetItemAsync<string>(ExpiresAtKey);

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expIso))
            return Anonymous();

        //refresh token
        var isTokenExpired = DateTimeOffset.TryParse(expIso, out var exp) && exp <= DateTimeOffset.UtcNow;
        if (isTokenExpired)
        {
            var refreshToken = await _localStorage.GetItemAsync<string>(RefreshTokenKey);
            if (refreshToken.IsNullOrEmpty())
                return Anonymous();

            var accessTokenResponse = await _apiClient.RefreshAsync(new RefreshRequest
            {
                RefreshToken = refreshToken!
            });

            await StoreAuthenticationState(accessTokenResponse.AccessToken, accessTokenResponse.ExpiresIn
                , accessTokenResponse.RefreshToken);
        }

        return await Authenticated();
    }

    public async Task MarkUserAsAuthenticated(string token, long expireIn, string refreshToken)
    {
        var authState = await CreateAndStoreAuthenticationState(token, expireIn, refreshToken);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync(AccessTokenKey);
        await _localStorage.RemoveItemAsync(ExpiresAtKey);
        await _localStorage.RemoveItemAsync(RefreshTokenKey);

        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
    }

    private async Task<AuthenticationState> CreateAndStoreAuthenticationState(string token, long expireIn, string refreshToken)
    {
        await _localStorage.SetItemAsync(AccessTokenKey, token);
        await _localStorage.SetItemAsync(RefreshTokenKey, refreshToken);

        var exp = DateTimeOffset.UtcNow.AddSeconds(expireIn).ToString("O");
        await _localStorage.SetItemAsync(ExpiresAtKey, exp);

        var claims = new List<Claim> { new(ClaimTypes.Name, "user") };
        var identity = new ClaimsIdentity(claims, "IdentityBearer");

        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    private async Task StoreAuthenticationState(string token, long expireIn, string refreshToken)
    {
        await _localStorage.SetItemAsync(AccessTokenKey, token);
        await _localStorage.SetItemAsync(RefreshTokenKey, refreshToken);

        var exp = DateTimeOffset.UtcNow.AddSeconds(expireIn).ToString("O");
        await _localStorage.SetItemAsync(ExpiresAtKey, exp);
    }

    private async Task<AuthenticationState> Authenticated()
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "user") };
        var identity = new ClaimsIdentity(claims, "IdentityBearer");

        var user = new ClaimsPrincipal(identity);

        return await Task.FromResult(new AuthenticationState(user));
    }

    private async Task ClearAsync()
    {
        await _localStorage.RemoveItemAsync(AccessTokenKey);
        await _localStorage.RemoveItemAsync(ExpiresAtKey);
        await _localStorage.RemoveItemAsync(RefreshTokenKey);
    }

    private static AuthenticationState Anonymous()
        => new(new ClaimsPrincipal(new ClaimsIdentity()));
}
