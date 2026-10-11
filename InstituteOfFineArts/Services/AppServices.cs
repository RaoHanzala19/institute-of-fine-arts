using System.Security.Claims;
using System.Text;
using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InstituteOfFineArts.Services;

public class InstituteOptions
{
    public string Name { get; set; } = "Institute of Fine Arts";
    public string ShortName { get; set; } = "IFA";
    public string Tagline { get; set; } = "Where every brushstroke finds its gallery";
    public string Address { get; set; } = "12 Gallery Road, Karachi, Pakistan";
    public string Phone { get; set; } = "+92 21 3555 0100";
    public string Email { get; set; } = "hello@institute-of-fine-arts.edu";
    public string Currency { get; set; } = "Rs";
    public decimal DefaultCommissionPercent { get; set; } = 10m;
    public bool SeedDemoData { get; set; } = true;
}

/// <summary>Adds the display name as a claim so it is available without hitting the database.</summary>
public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public AppClaimsPrincipalFactory(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options) : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("full_name", user.FullName));
        return identity;
    }
}

/// <summary>Creates in-app notifications. Callers save the context themselves.</summary>
public class NotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    public void Notify(string? userId, string title, string? message = null, string? url = null,
        NotificationType type = NotificationType.Info)
    {
        if (string.IsNullOrEmpty(userId)) return;
        _db.Notifications.Add(new Notification
        {
            UserId = userId, Title = title, Message = message, Url = url, Type = type
        });
    }

    public async Task NotifyStudentAsync(int studentId, string title, string? message = null, string? url = null,
        NotificationType type = NotificationType.Info)
    {
        var userId = await _db.Students.Where(s => s.StudentId == studentId)
            .Select(s => s.ApplicationUserId).FirstOrDefaultAsync();
        Notify(userId, title, message, url, type);
    }

    public async Task NotifyRoleAsync(string role, string title, string? message = null, string? url = null,
        NotificationType type = NotificationType.Info)
    {
        var ids = await (from ur in _db.UserRoles
                         join r in _db.Roles on ur.RoleId equals r.Id
                         join u in _db.Users on ur.UserId equals u.Id
                         where r.Name == role && u.IsActive
                         select u.Id).ToListAsync();
        foreach (var id in ids) Notify(id, title, message, url, type);
    }
}

/// <summary>Keeps competition / exhibition statuses in step with the calendar.</summary>
public class StatusSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<StatusSyncService> _log;

    public StatusSyncService(IServiceScopeFactory scopes, ILogger<StatusSyncService> log)
    {
        _scopes = scopes;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // small delay so the migration / seed at start-up finishes first
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SyncAsync(stoppingToken); }
            catch (Exception ex) { _log.LogWarning(ex, "Status sync failed"); }
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }

    public static async Task<int> SyncAsync(AppDbContext db, CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var changed = 0;

        // Competitions open on their start date. Concluding is a deliberate staff decision
        // (it opens the awards phase), so it is never automatic.
        foreach (var c in await db.Competitions.Where(c => c.Status == CompetitionStatus.Upcoming && c.StartDate <= now).ToListAsync(ct))
        {
            c.Status = CompetitionStatus.Ongoing;
            changed++;
        }

        foreach (var e in await db.Exhibitions
                     .Where(e => e.Status == ExhibitionStatus.Upcoming || e.Status == ExhibitionStatus.Ongoing).ToListAsync(ct))
        {
            var target = now.Date > e.ExhibitionEndDate.Date ? ExhibitionStatus.Concluded
                : now.Date >= e.ExhibitionStartDate.Date ? ExhibitionStatus.Ongoing
                : ExhibitionStatus.Upcoming;
            if (target != e.Status) { e.Status = target; changed++; }
        }

        if (changed > 0) await db.SaveChangesAsync(ct);
        return changed;
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var n = await SyncAsync(db, ct);
        if (n > 0) _log.LogInformation("Status sync updated {Count} record(s)", n);
    }
}

public static class Csv
{
    public static FileContentResult File(string fileName, IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', headers.Select(Cell)));
        foreach (var row in rows) sb.AppendLine(string.Join(',', row.Select(Cell)));
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return new FileContentResult(bytes, "text/csv; charset=utf-8")
        {
            FileDownloadName = $"{fileName}-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };
    }

    private static string Cell(object? value)
    {
        var s = value switch
        {
            null => "",
            DateTime d => d.ToString("yyyy-MM-dd HH:mm"),
            decimal m => m.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            Enum e => e.ToString(),
            _ => value.ToString() ?? ""
        };
        // neutralise spreadsheet formula injection
        if (s.Length > 0 && "=+-@\t\r".Contains(s[0])) s = "'" + s;
        return s.Contains(',') || s.Contains('"') || s.Contains('\n') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    }
}

public interface IPaged
{
    int PageIndex { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
}

public class PagedList<T> : List<T>, IPaged
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageIndex > 1;
    public bool HasNext => PageIndex < TotalPages;

    private PagedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = count;
        AddRange(items);
    }

    public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int page, int pageSize = 12)
    {
        page = Math.Max(1, page);
        var count = await source.CountAsync();
        var maxPage = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Min(page, maxPage);
        var items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedList<T>(items, count, page, pageSize);
    }
}

