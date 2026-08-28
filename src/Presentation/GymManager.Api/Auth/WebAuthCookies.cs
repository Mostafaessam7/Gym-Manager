namespace GymManager.Api.Auth;

/// <summary>
/// Carries the refresh token in an <c>HttpOnly</c> cookie instead of the JSON body, so it is
/// unreachable from JavaScript.
///
/// The frontend previously persisted the whole session — access token, refresh token and
/// permission claims — to <c>localStorage</c>. The access token is short-lived; the refresh token
/// is a renewable session, so anything able to read it could mint access tokens indefinitely. This
/// frontend is plain ES modules with no build step, which makes an injected script trivially able
/// to read Web Storage.
///
/// Moving a credential into a cookie means the browser attaches it automatically, which is exactly
/// what CSRF exploits — so the double-submit check below ships in the same change, not after it.
///
/// Transport is opt-in via <c>X-Auth-Transport: cookie</c>, matching PosFlow and RealEstateCRM.
/// Non-browser callers keep the body-based flow untouched.
/// </summary>
public static class WebAuthCookies
{
    public const string RefreshTokenCookieName = "gym_rt";

    /// <summary>
    /// Deliberately NOT HttpOnly: the frontend has to read this and echo it back in a header. An
    /// attacker's page can make the browser send cookies cross-origin, but the same-origin policy
    /// stops it reading them, so it cannot produce the matching header.
    /// </summary>
    public const string CsrfCookieName = "XSRF-TOKEN";

    public const string CsrfHeaderName = "X-XSRF-TOKEN";

    public const string TransportHeaderName = "X-Auth-Transport";

    public static bool UsesCookieTransport(HttpRequest request) =>
        string.Equals(request.Headers[TransportHeaderName], "cookie", StringComparison.OrdinalIgnoreCase)
        || request.Cookies.ContainsKey(RefreshTokenCookieName);

    public static bool HasValidCsrfToken(HttpRequest request)
    {
        var cookieValue = request.Cookies[CsrfCookieName];
        var headerValue = request.Headers[CsrfHeaderName].ToString();

        return !string.IsNullOrEmpty(cookieValue)
            && !string.IsNullOrEmpty(headerValue)
            // Ordinal, not culture-aware: these are opaque tokens, and culture-sensitive comparison
            // can treat distinct byte sequences as equal.
            && string.Equals(cookieValue, headerValue, StringComparison.Ordinal);
    }

    public static void Issue(HttpResponse response, string refreshToken, bool isDevelopment)
    {
        // SameSite=None is required when the frontend and API sit on different origins, and browsers
        // reject SameSite=None without Secure. Local development has no TLS, so it uses Lax —
        // differing ports do not change the SameSite "site" definition, so Lax works for a
        // localhost dev loop.
        var sameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None;
        var secure = !isDevelopment;
        var expires = DateTimeOffset.UtcNow.AddDays(30);

        response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Expires = expires,
            // Scoped to the auth endpoints: the refresh token has no business riding along on every
            // member, payment and report request.
            Path = "/api/v1/auth",
        });

        response.Cookies.Append(CsrfCookieName, Guid.NewGuid().ToString("N"), new CookieOptions
        {
            HttpOnly = false,
            Secure = secure,
            SameSite = sameSite,
            Expires = expires,
            Path = "/",
        });
    }

    public static void Clear(HttpResponse response, bool isDevelopment)
    {
        var sameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None;
        var secure = !isDevelopment;

        // Attributes must match those used when setting, or the browser treats this as a different
        // cookie and the original survives the logout.
        response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = "/api/v1/auth",
        });

        response.Cookies.Delete(CsrfCookieName, new CookieOptions
        {
            HttpOnly = false,
            Secure = secure,
            SameSite = sameSite,
            Path = "/",
        });
    }
}
