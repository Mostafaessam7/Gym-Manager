using GymManager.Application.Identity.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Login;

public sealed record LoginCommand(string Email, string Password, string? IpAddress = null, string? UserAgent = null)
    : ICommand<Result<LoginResponse>>;
