using GymManager.Application.Identity.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.RefreshAccessToken;

public sealed record RefreshAccessTokenCommand(string RefreshToken, string? IpAddress = null, string? UserAgent = null)
    : ICommand<Result<AuthenticationResponse>>;
