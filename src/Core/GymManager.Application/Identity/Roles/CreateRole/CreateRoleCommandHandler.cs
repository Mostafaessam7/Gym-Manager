using GymManager.Application.Identity.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Roles.CreateRole;

public sealed class CreateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        if (await roleRepository.NameExistsAsync(command.Name.Trim(), cancellationToken))
            return Result.Failure<RoleResponse>(RoleErrors.NameAlreadyInUse(command.Name));

        var role = Role.Create(command.Name, command.Description);

        foreach (var permission in command.Permissions.Where(Permissions.All.Contains))
            role.GrantPermission(permission);

        roleRepository.Add(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new RoleResponse(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.Permissions.Select(p => p.Code).ToArray()));
    }
}
