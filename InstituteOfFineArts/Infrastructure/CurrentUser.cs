using System.Security.Claims;

namespace InstituteOfFineArts.Infrastructure;

public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => Principal?.Identity?.IsAuthenticated == true
        ? Principal.FindFirstValue("full_name") ?? Principal.Identity!.Name
        : null;

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
