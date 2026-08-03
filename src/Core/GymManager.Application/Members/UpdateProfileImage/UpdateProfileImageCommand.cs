using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Members.UpdateProfileImage;

public sealed record UpdateProfileImageCommand(Guid MemberId, string? ImageUrl) : ICommand;
