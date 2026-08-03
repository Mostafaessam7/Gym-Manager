using System.Security.Claims;
using Asp.Versioning;
using GymManager.Api.Extensions;
using GymManager.Application.Identity.ChangePassword;
using GymManager.Application.Identity.Login;
using GymManager.Application.Identity.Logout;
using GymManager.Application.Identity.RefreshAccessToken;
using GymManager.Application.Identity.Register;
using GymManager.Application.Identity.RequestPasswordReset;
using GymManager.Application.Identity.ResendVerificationEmail;
using GymManager.Application.Identity.ResetPassword;
using GymManager.Application.Identity.Sessions.GetSessions;
using GymManager.Application.Identity.Sessions.RevokeAllSessions;
using GymManager.Application.Identity.Sessions.RevokeSession;
using GymManager.Application.Identity.TwoFactor.CompleteTwoFactorLogin;
using GymManager.Application.Identity.TwoFactor.ConfirmTwoFactorSetup;
using GymManager.Application.Identity.TwoFactor.DisableTwoFactor;
using GymManager.Application.Identity.TwoFactor.StartTwoFactorSetup;
using GymManager.Application.Identity.VerifyEmail;
using GymManager.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymManager.Api.Controllers.V1;

/// <summary>Registration, login, refresh-token rotation, logout, and the password-reset flow. Every action
/// here is anonymous by design — it's the entry point before a caller has a token.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName, Guid? BranchId);

    public sealed record LoginRequest(string Email, string Password);

    public sealed record RefreshTokenRequest(string RefreshToken);

    public sealed record RequestPasswordResetRequest(string Email);

    public sealed record ResetPasswordRequest(string Token, string NewPassword);

    public sealed record VerifyEmailRequest(string Token);

    public sealed record ResendVerificationEmailRequest(string Email);

    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    public sealed record CompleteTwoFactorLoginRequest(string ChallengeToken, string Code);

    public sealed record ConfirmTwoFactorSetupRequest(string Code);

    public sealed record DisableTwoFactorRequest(string CurrentPassword);

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(GymManager.Api.Extensions.ServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Email, request.Password, request.FirstName, request.LastName, request.BranchId);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(GymManager.Api.Extensions.ServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password, ClientIpAddress, ClientUserAgent);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshAccessTokenCommand(request.RefreshToken, ClientIpAddress, ClientUserAgent);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await dispatcher.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return NoContent();
    }

    [HttpPost("password-reset/request")]
    [AllowAnonymous]
    [EnableRateLimiting(GymManager.Api.Extensions.ServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> RequestPasswordReset(RequestPasswordResetRequest request, CancellationToken cancellationToken)
    {
        await dispatcher.Send(new RequestPasswordResetCommand(request.Email), cancellationToken);
        return NoContent();
    }

    [HttpPost("password-reset/confirm")]
    [AllowAnonymous]
    [EnableRateLimiting(GymManager.Api.Extensions.ServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new VerifyEmailCommand(request.Token), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("verify-email/resend")]
    [AllowAnonymous]
    [EnableRateLimiting(GymManager.Api.Extensions.ServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> ResendVerificationEmail(ResendVerificationEmailRequest request, CancellationToken cancellationToken)
    {
        await dispatcher.Send(new ResendVerificationEmailCommand(request.Email), cancellationToken);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await dispatcher.Send(new GetSessionsQuery(userId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await dispatcher.Send(new RevokeSessionCommand(userId, sessionId), cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("sessions/revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await dispatcher.Send(new RevokeAllSessionsCommand(userId), cancellationToken);

        return NoContent();
    }

    [HttpPost("login/2fa")]
    [AllowAnonymous]
    [EnableRateLimiting(GymManager.Api.Extensions.ServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> CompleteTwoFactorLogin(CompleteTwoFactorLoginRequest request, CancellationToken cancellationToken)
    {
        var command = new CompleteTwoFactorLoginCommand(request.ChallengeToken, request.Code, ClientIpAddress, ClientUserAgent);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("2fa/setup")]
    [Authorize]
    public async Task<IActionResult> StartTwoFactorSetup(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await dispatcher.Send(new StartTwoFactorSetupCommand(userId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("2fa/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmTwoFactorSetup(ConfirmTwoFactorSetupRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await dispatcher.Send(new ConfirmTwoFactorSetupCommand(userId, request.Code), cancellationToken);

        return result.IsSuccess ? Ok(new { recoveryCodes = result.Value }) : result.ToProblemDetails();
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor(DisableTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await dispatcher.Send(new DisableTwoFactorCommand(userId, request.CurrentPassword), cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    private string? ClientIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? ClientUserAgent => Request.Headers.UserAgent.ToString() is { Length: > 0 } userAgent ? userAgent : null;
}
