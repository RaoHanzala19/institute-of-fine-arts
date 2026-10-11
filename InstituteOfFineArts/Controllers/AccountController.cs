using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly AppDbContext _db;

    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, AppDbContext db)
    {
        _users = users;
        _signIn = signIn;
        _db = db;
    }

    // ── Sign in / out ──────────────────────────────────────────────────────────
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByEmailAsync(model.Email.Trim());
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError("", "We could not sign you in with those details, or the account is inactive.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", "Too many failed attempts. The account is locked for 10 minutes.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "We could not sign you in with those details, or the account is inactive.");
            return View(model);
        }

        user.LastLoginAt = DateTime.Now;
        await _users.UpdateAsync(user);
        _db.AddAudit("Login", "Account", user.Id, user.Email, user.Id, user.FullName);
        await _db.SaveChangesAsync();

        return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl)
            : RedirectToAction("Index", "Dashboard");
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ── Profile ────────────────────────────────────────────────────────────────
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        ViewData["IsPortal"] = true;
        return View(await BuildProfileAsync());
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm model)
    {
        ViewData["IsPortal"] = true;
        var user = await _db.Users.Include(u => u.Staff).Include(u => u.Student)
            .FirstAsync(u => u.Id == _users.GetUserId(User));

        if (!ModelState.IsValid)
        {
            model.Email = user.Email ?? "";
            model.Role = (await _users.GetRolesAsync(user)).FirstOrDefault() ?? "";
            model.HasProfileRecord = user.Staff != null || user.Student != null;
            return View(model);
        }

        user.FullName = $"{model.FirstName.Trim()} {model.LastName.Trim()}";
        user.PhoneNumber = model.PhoneNumber;

        if (user.Staff is { } staff)
        {
            staff.FirstName = model.FirstName.Trim();
            staff.LastName = model.LastName.Trim();
            staff.PhoneNumber = model.PhoneNumber ?? staff.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(model.Address)) staff.Address = model.Address;
            staff.UpdatedAt = DateTime.Now;
        }
        else if (user.Student is { } student)
        {
            student.FirstName = model.FirstName.Trim();
            student.LastName = model.LastName.Trim();
            student.PhoneNumber = model.PhoneNumber ?? student.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(model.Address)) student.Address = model.Address;
            student.UpdatedAt = DateTime.Now;
        }

        await _users.UpdateAsync(user);
        await _signIn.RefreshSignInAsync(user);   // refreshes the full_name claim

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    public IActionResult ChangePassword()
    {
        ViewData["IsPortal"] = true;
        return View(new ChangePasswordVm());
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm model)
    {
        ViewData["IsPortal"] = true;
        if (!ModelState.IsValid) return View(model);

        var user = (await _users.GetUserAsync(User))!;
        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }

        _db.AddAudit("PasswordChanged", "Account", user.Id, user.Email);
        await _db.SaveChangesAsync();
        await _signIn.RefreshSignInAsync(user);

        TempData["Success"] = "Your password has been changed.";
        return RedirectToAction(nameof(Profile));
    }

    private async Task<ProfileVm> BuildProfileAsync()
    {
        var user = await _db.Users.Include(u => u.Staff).Include(u => u.Student)
            .FirstAsync(u => u.Id == _users.GetUserId(User));

        var parts = user.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return new ProfileVm
        {
            FirstName = user.Staff?.FirstName ?? user.Student?.FirstName ?? parts.ElementAtOrDefault(0) ?? "",
            LastName = user.Staff?.LastName ?? user.Student?.LastName ?? parts.ElementAtOrDefault(1) ?? "",
            Email = user.Email ?? "",
            Role = (await _users.GetRolesAsync(user)).FirstOrDefault() ?? "",
            PhoneNumber = user.PhoneNumber ?? user.Staff?.PhoneNumber ?? user.Student?.PhoneNumber,
            Address = user.Staff?.Address ?? user.Student?.Address,
            HasProfileRecord = user.Staff != null || user.Student != null
        };
    }
}
