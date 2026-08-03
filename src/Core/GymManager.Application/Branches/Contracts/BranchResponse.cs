namespace GymManager.Application.Branches.Contracts;

public sealed record BranchResponse(
    Guid Id,
    string Name,
    string? Street,
    string? City,
    string? State,
    string? PostalCode,
    string Country,
    string? PhoneNumber,
    string? Email,
    bool IsActive);
