using GymManager.Application.Abstractions;

namespace GymManager.Infrastructure.Authentication;

/// <inheritdoc cref="IPasswordHasher"/>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
}
