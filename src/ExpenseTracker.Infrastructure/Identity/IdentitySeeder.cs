using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>
/// Idempotent startup seeding: the built-in roles with their default permissions, the bootstrap
/// SuperAdmin and (Development only) demo users. Existing data is never overwritten, so permission
/// changes made by administrators survive restarts.
/// </summary>
public sealed partial class IdentitySeeder(
    UserManager<AppUser> users,
    RoleManager<AppRole> roles,
    AppDbContext db,
    IOptions<IdentityStoreOptions> settings,
    TimeProvider clock,
    ILogger<IdentitySeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var rolesCreated = await SeedRolesAsync(ct);
        var usersCreated = 0;

        var seed = settings.Value.Seed;
        if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            LogSeedSkipped(logger, "no bootstrap administrator configured (Identity:Seed:AdminEmail / AdminPassword)");
        }
        else if (await SeedUserAsync(seed.AdminEmail, seed.AdminPassword, seed.AdminDisplayName, AppRoles.SuperAdmin))
        {
            usersCreated++;
        }

        if (seed.ShouldSeedDemoUsers && !string.IsNullOrWhiteSpace(seed.AdminEmail) && !string.IsNullOrWhiteSpace(seed.DemoPassword))
        {
            var domain = seed.AdminEmail[(seed.AdminEmail.IndexOf('@') + 1)..];
            foreach (var role in new[] { AppRoles.Admin, AppRoles.User, AppRoles.Reader })
            {
                if (await SeedUserAsync($"{role.ToLowerInvariant()}@{domain}", seed.DemoPassword, $"Demo {role}", role))
                    usersCreated++;
            }
        }

        LogSeeded(logger, rolesCreated, usersCreated);
    }

    private async Task<int> SeedRolesAsync(CancellationToken ct)
    {
        var created = 0;
        foreach (var definition in AppRoles.All)
        {
            var role = await roles.FindByNameAsync(definition.Name);
            if (role is null)
            {
                role = new AppRole(definition.Name) { Description = definition.Description };
                var result = await roles.CreateAsync(role);
                if (!result.Succeeded) throw new InvalidOperationException($"Could not create role {definition.Name}: {result}");

                db.RoleClaims.AddRange(definition.DefaultPermissions.Select(p => NewPermission(role.Id, p)));
                created++;
            }
            else if (role.Description != definition.Description)
            {
                role.Description = definition.Description;
            }
        }
        await db.SaveChangesAsync(ct);

        // Drop permissions that no longer exist in the catalog (renamed or removed in code).
        var stale = (await db.RoleClaims.Where(c => c.ClaimType == AppClaimTypes.Permission).ToListAsync(ct))
            .Where(c => !Permissions.IsKnown(c.ClaimValue ?? ""))
            .ToList();
        db.RoleClaims.RemoveRange(stale);

        // Keep SuperAdmin's stored permissions complete (its effective permissions are all of them regardless).
        var superAdmin = await roles.FindByNameAsync(AppRoles.SuperAdmin);
        if (superAdmin is not null)
        {
            var held = await db.RoleClaims
                .Where(c => c.RoleId == superAdmin.Id && c.ClaimType == AppClaimTypes.Permission)
                .Select(c => c.ClaimValue!)
                .ToListAsync(ct);
            db.RoleClaims.AddRange(Permissions.All.Select(p => p.Name).Except(held).Select(p => NewPermission(superAdmin.Id, p)));
        }

        await db.SaveChangesAsync(ct);
        return created;
    }

    /// <returns>Whether the user was created (false when it already existed).</returns>
    private async Task<bool> SeedUserAsync(string email, string password, string displayName, string role)
    {
        if (await users.FindByEmailAsync(email) is not null) return false;

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = clock.GetUtcNow(),
        };

        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not seed user {email}: {string.Join(" ", result.Errors.Select(e => e.Description))}");

        await users.AddToRoleAsync(user, role);
        return true;
    }

    private static IdentityRoleClaim<Guid> NewPermission(Guid roleId, string permission) =>
        new() { RoleId = roleId, ClaimType = AppClaimTypes.Permission, ClaimValue = permission };

    [LoggerMessage(EventId = LogEvents.IdentitySeeded, Level = LogLevel.Information,
        Message = "Identity seeded: {RolesCreated} role(s) and {UsersCreated} user(s) created")]
    private static partial void LogSeeded(ILogger logger, int rolesCreated, int usersCreated);

    [LoggerMessage(EventId = LogEvents.IdentitySeedSkipped, Level = LogLevel.Warning,
        Message = "Skipped seeding the bootstrap administrator: {Reason}")]
    private static partial void LogSeedSkipped(ILogger logger, string reason);
}
