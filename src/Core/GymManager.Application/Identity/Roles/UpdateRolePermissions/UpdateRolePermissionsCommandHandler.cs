using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Roles.UpdateRolePermissions;

public sealed class UpdateRolePermissionsCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateRolePermissionsCommand>
{
    public async Task<Result> Handle(UpdateRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        foreach (var code in role.Permissions.Select(p => p.Code).ToArray())
            if (!command.Permissions.Contains(code))
                role.RevokePermission(code);

        foreach (var code in command.Permissions.Where(Permissions.All.Contains))
            role.GrantPermission(code);

        roleRepository.Update(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
