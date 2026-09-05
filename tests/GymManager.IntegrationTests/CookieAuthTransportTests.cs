using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>
/// Covers the HttpOnly-cookie auth transport.
///
/// These matter more here than in the other projects because this frontend has no automated tests
/// at all and no build step — a mistake in the auth flow would otherwise reach a browser before
/// anything noticed. The backend contract is the only place it can be pinned.
///
/// The property that matters most is the negative one: after a cookie login the refresh token must
/// NOT be in the response body. Returning it in both places would look identical to a working
/// implementation while protecting nothing.
/// </summary>
public sealed class CookieAuthTransportTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record AuthResponse(Guid UserId, string Email, string AccessToken, string RefreshToken);

    private sealed record LoginResponseDto(bool RequiresTwoFactor, string? TwoFactorChallengeToken, AuthResponse? Authentication);

    private static async Task<(HttpResponseMessage Response, string Email, string Password)> RegisterAsync(HttpClient client)
    {
        var email = $"cookie-{Guid.NewGuid():N}@gym.io";
        const string password = "Password123";

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password, firstName = "Cookie", lastName = "Transport" });

        return (response, email, password);
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password, bool cookieTransport)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email, password }),
        };

        if (cookieTransport)
        {
            request.Headers.Add("X-Auth-Transport", "cookie");
        }

        return await client.SendAsync(request);
    }

    private static IEnumerable<string> SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [];

    [Fact]
    public async Task Body_transport_is_unchanged_without_the_opt_in_header()
    {
        // Every other test in this suite, and any non-browser caller, relies on this. The change is
        // additive or it is a breaking change wearing a disguise.
        var client = factory.CreateClient();
        var (registerResponse, email, password) = await RegisterAsync(client);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var response = await LoginAsync(client, email, password, cookieTransport: false);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body?.Authentication);
        Assert.False(string.IsNullOrEmpty(body!.Authentication!.RefreshToken));
        Assert.DoesNotContain(SetCookies(response), c => c.StartsWith("gym_rt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Cookie_login_sets_an_HttpOnly_refresh_cookie_and_omits_it_from_the_body()
    {
        var client = factory.CreateClient();
        var (registerResponse, email, password) = await RegisterAsync(client);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var response = await LoginAsync(client, email, password, cookieTransport: true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refreshCookie = SetCookies(response)
            .FirstOrDefault(c => c.StartsWith("gym_rt", StringComparison.Ordinal));

        Assert.NotNull(refreshCookie);

        // HttpOnly is the entire point: an injected script cannot read this even after hooking fetch.
        Assert.Contains("httponly", refreshCookie, StringComparison.OrdinalIgnoreCase);

        // Scoped to the auth endpoints — the refresh token has no reason to ride along on every
        // member, payment and report request.
        Assert.Contains("path=/api/v1/auth", refreshCookie, StringComparison.OrdinalIgnoreCase);

        // The regression that would make this change cosmetic while appearing to work.
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(body?.Authentication);
        Assert.Equal(string.Empty, body!.Authentication!.RefreshToken);

        // The access token still comes back in the body: it is short-lived and the client holds it
        // in memory only.
        Assert.False(string.IsNullOrEmpty(body.Authentication.AccessToken));
    }

    [Fact]
    public async Task Csrf_cookie_is_script_readable_unlike_the_refresh_cookie()
    {
        // Deliberately not HttpOnly — the client must read it and echo it back in a header. That
        // asymmetry is the whole double-submit mechanism.
        var client = factory.CreateClient();
        var (_, email, password) = await RegisterAsync(client);

        var response = await LoginAsync(client, email, password, cookieTransport: true);

        var csrfCookie = SetCookies(response)
            .FirstOrDefault(c => c.StartsWith("XSRF-TOKEN", StringComparison.Ordinal));

        Assert.NotNull(csrfCookie);
        Assert.DoesNotContain("httponly", csrfCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_succeeds_with_the_empty_body_the_browser_client_actually_sends()
    {
        // Every other test in this file posts `refreshToken = ""`. The real frontend posts `{}`
        // with no such key -- and those are not the same request. An empty string satisfies
        // [ApiController]'s implicit required check for a non-nullable string; an absent key does
        // not, and used to fail with 400 "The RefreshToken field is required" before the action
        // ever ran, making the cookie branch of ResolveRefreshToken unreachable.
        //
        // The effect was that login worked, the browser navigated to dashboard.html, the dashboard
        // refreshed, got a 400, cleared the session and bounced straight back to the login page.
        // The app could not be used at all, while this suite stayed green.
        var client = factory.CreateClient();
        var (registerResponse, email, password) = await RegisterAsync(client);
        registerResponse.EnsureSuccessStatusCode();

        var login = await LoginAsync(client, email, password, cookieTransport: true);
        login.EnsureSuccessStatusCode();

        var cookies = SetCookies(login)
            .Select(c => c.Split(';')[0])
            .ToList();
        var csrf = cookies
            .First(c => c.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
            .Split('=', 2)[1];

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            // Exactly what js/api/apiClient.js sends: JSON.stringify({}).
            Content = JsonContent.Create(new { }),
        };
        request.Headers.Add("Cookie", string.Join("; ", cookies));
        request.Headers.Add("X-Auth-Transport", "cookie");
        request.Headers.Add("X-XSRF-TOKEN", csrf);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refreshed = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(refreshed!.AccessToken));

        // Cookie transport means the rotated refresh token goes back in the cookie, never the body.
        Assert.True(string.IsNullOrEmpty(refreshed.RefreshToken));
    }

    [Fact]
    public async Task Logout_accepts_the_empty_body_the_browser_client_actually_sends()
    {
        // Same shape, same trap: Logout binds the same request record.
        var client = factory.CreateClient();
        var (registerResponse, email, password) = await RegisterAsync(client);
        registerResponse.EnsureSuccessStatusCode();

        var login = await LoginAsync(client, email, password, cookieTransport: true);
        login.EnsureSuccessStatusCode();

        var cookies = SetCookies(login).Select(c => c.Split(';')[0]).ToList();
        var csrf = cookies
            .First(c => c.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
            .Split('=', 2)[1];

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new { }),
        };
        request.Headers.Add("Cookie", string.Join("; ", cookies));
        request.Headers.Add("X-Auth-Transport", "cookie");
        request.Headers.Add("X-XSRF-TOKEN", csrf);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_carrying_the_cookie_without_a_CSRF_header_is_rejected()
    {
        // Without this the change would trade XSS exposure for CSRF exposure: the browser attaches
        // the cookie to a cross-origin form post automatically.
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = "" }),
        };
        request.Headers.Add("Cookie", "gym_rt=some-token; XSRF-TOKEN=abc123");
        // No X-XSRF-TOKEN header — this is the forged-request shape.

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_with_a_mismatched_CSRF_header_is_rejected()
    {
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = "" }),
        };
        request.Headers.Add("Cookie", "gym_rt=some-token; XSRF-TOKEN=abc123");
        request.Headers.Add("X-XSRF-TOKEN", "a-different-value");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Logout_carrying_the_cookie_without_a_CSRF_header_is_rejected()
    {
        // Logout is state-changing too: a forged logout is a denial-of-service on the user's session.
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new { refreshToken = "" }),
        };
        request.Headers.Add("Cookie", "gym_rt=some-token; XSRF-TOKEN=abc123");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
