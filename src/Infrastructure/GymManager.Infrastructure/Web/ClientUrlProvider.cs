using GymManager.Application.Abstractions;

namespace GymManager.Infrastructure.Web;

/// <inheritdoc cref="IClientUrlProvider"/>
public sealed class ClientUrlProvider(ClientOptions options) : IClientUrlProvider
{
    public string BaseUrl => options.BaseUrl.TrimEnd('/');
}

public sealed class ClientOptions
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; init; } = "http://localhost:5500";
}
