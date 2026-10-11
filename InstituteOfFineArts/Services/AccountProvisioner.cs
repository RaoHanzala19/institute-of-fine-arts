using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Services;

/// <summary>Creates and maintains Identity accounts so controllers do not repeat the same plumbing.</summary>
public class AccountProvisioner
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly AppDbContext _db;

    public AccountProvisioner(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, AppDbContext db)
    {
        _users = users;
        _roles = roles;
        _db = db;
    }

    public async Task<(ApplicationUser? User, IReadOnlyList<string> Errors)> CreateAsync(
        string fullName, string email, string password, string role)
    {
        email = email.Trim();
        if (await _users.FindByEmailAsync(email) is not null)
            return (null, new[] { "An account with this email address already exists." });

        var user = new ApplicationUser
        {
            UserName = email, Email = email, EmailConfirmed = true, FullName = fullName.Trim(), IsActive = true
        };

        var result = await _users.CreateAsync(user, password);
        if (!result.Succeeded) return (null, result.Errors.Select(e => e.Description).ToList());

        if (!await _roles.RoleExistsAsync(role)) await _roles.CreateAsync(new IdentityRole(role));
        await _users.AddToRoleAsync(user, role);
        return (user, Array.Empty<string>());
    }

    /// <summary>Removes a freshly created account when the profile row that should accompany it fails.</summary>
    public async Task RollbackAsync(ApplicationUser user) => await _users.DeleteAsync(user);

    public async Task SetActiveAsync(string userId, bool active)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return;
        user.IsActive = active;
        await _users.UpdateAsync(user);
        if (!active) await _users.UpdateSecurityStampAsync(user);   // forces sign-out on the next request
    }

    public async Task<IReadOnlyList<string>> ResetPasswordAsync(string userId, string newPassword)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return new[] { "User not found." };

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var result = await _users.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded) return result.Errors.Select(e => e.Description).ToList();

        await _users.ResetAccessFailedCountAsync(user);
        await _users.SetLockoutEndDateAsync(user, null);
        return Array.Empty<string>();
    }

    public async Task<int> ActiveAdminCountAsync()
    {
        var adminRoleId = await _db.Roles.Where(r => r.Name == Roles.Admin).Select(r => r.Id).FirstOrDefaultAsync();
        if (adminRoleId is null) return 0;
        return await (from ur in _db.UserRoles join u in _db.Users on ur.UserId equals u.Id
                      where ur.RoleId == adminRoleId && u.IsActive select u.Id).CountAsync();
    }
}
