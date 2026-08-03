namespace GymManager.Application.Abstractions;

/// <summary>Exposes the base URL of the deployed frontend, for building links embedded in outbound email
/// (password reset, etc.). Backed by configuration in Infrastructure rather than hard-coded, since the
/// frontend origin differs between local docker-compose and any real deployment.</summary>
public interface IClientUrlProvider
{
    string BaseUrl { get; }
}
