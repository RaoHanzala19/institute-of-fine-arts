using System.Security.Claims;
using InstituteOfFineArts.Data;
using InstituteOfFineArts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InstituteOfFineArts.Controllers;

/// <summary>Base class for every signed-in screen: portal layout, flash messages, current user helpers.</summary>
[Authorize]
public abstract class PortalController : Controller
{
    protected readonly AppDbContext Db;

    protected PortalController(AppDbContext db) => Db = db;

    protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewData["IsPortal"] = true;
        base.OnActionExecuting(context);
    }

    protected void Success(string message) => TempData["Success"] = message;
    protected void Error(string message) => TempData["Error"] = message;
    protected void Warn(string message) => TempData["Warning"] = message;

    protected IActionResult ToReturnUrl(string? returnUrl, string action, object? routeValues = null) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(action, routeValues);
}
