using GymManager.Application.Abstractions;
using GymManager.Domain.Branches;
using GymManager.Domain.Common;
using GymManager.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymManager.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the built-in system roles (Owner, Manager, Front Desk, Trainer), a default Owner account and a
/// default branch on first run against an empty database. Runtime seeding rather than migration
/// <c>HasData</c> is used here so the admin password can be hashed with the environment's configured
/// work factor instead of a hard-coded value baked into a migration.
///
/// The default owner email/password (<see cref="DefaultOwnerEmail"/>/<see cref="DefaultOwnerPassword"/>)
/// are public, checked-in values. <c>SecretsValidator</c> (in the API project) already refuses to start a
/// non-Development/Testing deployment unless <c>Seed:AdminPassword</c> has been overridden away from that
/// default, so by the time this method runs in such an environment, <c>Seed:AdminEmail</c>/
/// <c>Seed:AdminPassword</c> (if set) are safe to trust as the real initial owner credentials.
/// </summary>
public static class DataSeeder
{
    private const string OwnerRoleName = "Owner";
    public const string DefaultOwnerEmail = "admin@gymmanager.local";
    public const string DefaultOwnerPassword = "Admin@12345";
    private const string DefaultBranchName = "Main Branch";

    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

        var ownerEmail = configuration["Seed:AdminEmail"] is { Length: > 0 } configuredEmail ? configuredEmail : DefaultOwnerEmail;
        var ownerPassword = configuration["Seed:AdminPassword"] is { Length: > 0 } configuredPassword ? configuredPassword : DefaultOwnerPassword;

        await dbContext.Database.MigrateAsync(cancellationToken);

        if (!await dbContext.Roles.AnyAsync(cancellationToken))
        {
            var systemRoles = new (string Name, string Description, string[] Permissions)[]
            {
                (OwnerRoleName, "Full, unrestricted access to every module.", [.. Permissions.All]),
                ("Manager", "Day-to-day operational management, excluding role and settings administration.",
                    Permissions.All.Where(p => !p.StartsWith("roles:") && !p.StartsWith("settings:")).ToArray()),
                ("Front Desk", "Check-in, member lookup and point-of-sale operations.",
                [
                    Permissions.Members.View, Permissions.Attendance.View, Permissions.Attendance.CheckIn,
                    Permissions.Memberships.View, Permissions.Classes.View, Permissions.Classes.Book,
                    Permissions.Pos.Sell, Permissions.Payments.View, Permissions.Payments.Process,
                    Permissions.Lockers.View, Permissions.Lockers.Manage, Permissions.Dashboard.View,
                    Permissions.Crm.View, Permissions.Crm.Manage,
                    Permissions.GiftCards.View, Permissions.GiftCards.Manage,
                ]),
                ("Trainer", "Class and personal schedule management.",
                [
                    Permissions.Trainers.View, Permissions.Classes.View, Permissions.Classes.Manage,
                    Permissions.Members.View, Permissions.Attendance.View, Permissions.Dashboard.View,
                    Permissions.Workouts.View, Permissions.Workouts.Manage,
                    Permissions.Nutrition.View, Permissions.Nutrition.Manage,
                    Permissions.Staff.View,
                ]),
            };

            foreach (var (name, description, permissions) in systemRoles)
            {
                var role = Role.Create(name, description, isSystemRole: true);
                foreach (var permission in permissions)
                    role.GrantPermission(permission);

                dbContext.Roles.Add(role);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} system roles", systemRoles.Length);
        }

        if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            var ownerRole = await dbContext.Roles.FirstAsync(r => r.Name == OwnerRoleName, cancellationToken);

            var emailResult = Email.Create(ownerEmail);
            var owner = User.Register(emailResult.Value, passwordHasher.Hash(ownerPassword), "System", "Owner");
            owner.AssignRole(ownerRole.Id);
            owner.MarkEmailVerified();

            dbContext.Users.Add(owner);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (ownerPassword == DefaultOwnerPassword)
            {
                logger.LogWarning(
                    "Seeded default owner account {Email} with a well-known password. Change it immediately in non-development environments.",
                    ownerEmail);
            }
            else
            {
                logger.LogInformation("Seeded owner account {Email} from Seed:AdminEmail/Seed:AdminPassword configuration.", ownerEmail);
            }
        }

        if (!await dbContext.Branches.AnyAsync(cancellationToken))
        {
            var address = Address.Create("USA");
            var branch = Branch.Create(DefaultBranchName, address, phoneNumber: null, email: null);

            dbContext.Branches.Add(branch);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeded default branch {BranchName}", DefaultBranchName);
        }
    }
}
