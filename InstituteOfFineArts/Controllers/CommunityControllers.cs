using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Services;
using InstituteOfFineArts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Controllers;

// ══ Announcements ═════════════════════════════════════════════════════════════
[Authorize(Roles = "Admin,Manager")]
public class AnnouncementsController : PortalController
{
    public AnnouncementsController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index() =>
        View(await Db.Announcements.AsNoTracking().Include(a => a.CreatedBy)
            .OrderByDescending(a => a.IsPinned).ThenByDescending(a => a.PublishedAt).ToListAsync());

    public IActionResult Create() => View("Form", new AnnouncementFormVm());

    [HttpPost]
    public async Task<IActionResult> Create(AnnouncementFormVm vm)
    {
        if (!ModelState.IsValid) return View("Form", vm);
        Db.Announcements.Add(new Announcement
        {
            Title = vm.Title.Trim(), Body = vm.Body.Trim(), Audience = vm.Audience, IsPinned = vm.IsPinned,
            ExpiresAt = vm.ExpiresAt?.Date.AddDays(1).AddSeconds(-1), CreatedById = UserId
        });
        await Db.SaveChangesAsync();
        Success("Announcement published.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var a = await Db.Announcements.AsNoTracking().FirstOrDefaultAsync(x => x.AnnouncementId == id);
        if (a is null) return NotFound();
        return View("Form", new AnnouncementFormVm
        {
            AnnouncementId = a.AnnouncementId, Title = a.Title, Body = a.Body, Audience = a.Audience,
            IsPinned = a.IsPinned, ExpiresAt = a.ExpiresAt?.Date
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, AnnouncementFormVm vm)
    {
        if (id != vm.AnnouncementId) return BadRequest();
        if (!ModelState.IsValid) return View("Form", vm);
        var a = await Db.Announcements.FirstOrDefaultAsync(x => x.AnnouncementId == id);
        if (a is null) return NotFound();
        a.Title = vm.Title.Trim(); a.Body = vm.Body.Trim(); a.Audience = vm.Audience; a.IsPinned = vm.IsPinned;
        a.ExpiresAt = vm.ExpiresAt?.Date.AddDays(1).AddSeconds(-1);
        await Db.SaveChangesAsync();
        Success("Announcement updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await Db.Announcements.FirstOrDefaultAsync(x => x.AnnouncementId == id);
        if (a is null) return NotFound();
        Db.Announcements.Remove(a);
        await Db.SaveChangesAsync();
        Success("Announcement removed.");
        return RedirectToAction(nameof(Index));
    }
}

// ══ Notifications ═════════════════════════════════════════════════════════════
public class NotificationsController : PortalController
{
    public NotificationsController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(bool unread = false, int page = 1)
    {
        var q = Db.Notifications.AsNoTracking().Where(n => n.UserId == UserId);
        if (unread) q = q.Where(n => !n.IsRead);
        ViewBag.Unread = unread;
        return View(await PagedList<Notification>.CreateAsync(q.OrderByDescending(n => n.CreatedAt), page, 15));
    }

    /// <summary>Marks a notification as read and forwards to its target page.</summary>
    public async Task<IActionResult> Open(int id)
    {
        var n = await Db.Notifications.FirstOrDefaultAsync(x => x.NotificationId == id && x.UserId == UserId);
        if (n is null) return NotFound();
        n.IsRead = true;
        await Db.SaveChangesAsync();
        return !string.IsNullOrEmpty(n.Url) && Url.IsLocalUrl(n.Url) ? LocalRedirect(n.Url) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllRead()
    {
        await Db.Notifications.Where(n => n.UserId == UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        Success("All notifications marked as read.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        await Db.Notifications.Where(n => n.UserId == UserId && n.IsRead).ExecuteDeleteAsync();
        Success("Read notifications cleared.");
        return RedirectToAction(nameof(Index));
    }
}

public class NotificationBellViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public NotificationBellViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var uid = UserClaimsPrincipal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (uid is null) return Content(string.Empty);

        var unread = await _db.Notifications.CountAsync(n => n.UserId == uid && !n.IsRead);
        var latest = await _db.Notifications.AsNoTracking().Where(n => n.UserId == uid)
            .OrderByDescending(n => n.CreatedAt).Take(5).ToListAsync();
        ViewData["Unread"] = unread;
        return View(latest);
    }
}

// ══ Contact inbox ═════════════════════════════════════════════════════════════
[Authorize(Roles = "Admin,Manager")]
public class MessagesController : PortalController
{
    public MessagesController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? filter, int page = 1)
    {
        var q = Db.ContactMessages.AsNoTracking().AsQueryable();
        q = filter switch
        {
            "unread" => q.Where(m => !m.IsRead),
            "open" => q.Where(m => !m.IsResolved),
            "resolved" => q.Where(m => m.IsResolved),
            _ => q
        };
        ViewBag.Filter = filter;
        return View(await PagedList<ContactMessage>.CreateAsync(q.OrderByDescending(m => m.CreatedAt), page, 12));
    }

    public async Task<IActionResult> Details(int id)
    {
        var m = await Db.ContactMessages.FirstOrDefaultAsync(x => x.ContactMessageId == id);
        if (m is null) return NotFound();
        if (!m.IsRead) { m.IsRead = true; await Db.SaveChangesAsync(); }
        return View(m);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleResolved(int id)
    {
        var m = await Db.ContactMessages.FirstOrDefaultAsync(x => x.ContactMessageId == id);
        if (m is null) return NotFound();
        m.IsResolved = !m.IsResolved;
        await Db.SaveChangesAsync();
        Success(m.IsResolved ? "Marked as resolved." : "Re-opened.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await Db.ContactMessages.FirstOrDefaultAsync(x => x.ContactMessageId == id);
        if (m is null) return NotFound();
        Db.ContactMessages.Remove(m);
        await Db.SaveChangesAsync();
        Success("Message deleted.");
        return RedirectToAction(nameof(Index));
    }
}

// ══ Audit trail ═══════════════════════════════════════════════════════════════
[Authorize(Roles = Roles.Admin)]
public class AuditController : PortalController
{
    public AuditController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, string? act, string? entity, DateTime? from, DateTime? to,
        int page = 1, bool export = false)
    {
        var query = Db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(a => a.UserName.Contains(q) || (a.Details != null && a.Details.Contains(q)) || (a.EntityId != null && a.EntityId == q));
        if (!string.IsNullOrWhiteSpace(act)) query = query.Where(a => a.Action == act);
        if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(a => a.EntityName == entity);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from);
        if (to.HasValue) query = query.Where(a => a.Timestamp < to.Value.Date.AddDays(1));
        query = query.OrderByDescending(a => a.Timestamp);

        if (export)
        {
            var all = await query.Take(5000).ToListAsync();
            return Csv.File("audit-log",
                new[] { "When", "User", "Action", "Entity", "Id", "Details", "IP" },
                all.Select(a => new object?[] { a.Timestamp, a.UserName, a.Action, a.EntityName, a.EntityId, a.Details, a.IpAddress }));
        }

        ViewBag.Q = q; ViewBag.Act = act; ViewBag.Entity = entity; ViewBag.From = from; ViewBag.To = to;
        ViewBag.Actions = new SelectList(await Db.AuditLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync(), act);
        ViewBag.Entities = new SelectList(await Db.AuditLogs.Select(a => a.EntityName).Distinct().OrderBy(a => a).ToListAsync(), entity);
        return View(await PagedList<AuditLog>.CreateAsync(query, page, 25));
    }
}

// ══ Public gallery ════════════════════════════════════════════════════════════
public class GalleryController : Controller
{
    private readonly AppDbContext _db;

    public GalleryController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, int? competitionId, string sort = "new", int page = 1)
    {
        var query = _db.Submissions.AsNoTracking().Include(s => s.Student).Include(s => s.Competition).Include(s => s.Likes)
            .Where(s => s.Grade == SubmissionGrade.Best);
        if (competitionId.HasValue) query = query.Where(s => s.CompetitionId == competitionId);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.PaintingTitle.Contains(q) || s.Student.FirstName.Contains(q) ||
                                     s.Student.LastName.Contains(q) || s.Competition.Title.Contains(q));

        query = sort switch
        {
            "liked" => query.OrderByDescending(s => s.Likes.Count).ThenByDescending(s => s.SubmittedAt),
            "title" => query.OrderBy(s => s.PaintingTitle),
            _ => query.OrderByDescending(s => s.SubmittedAt)
        };

        var uid = User.Identity?.IsAuthenticated == true ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
        var items = await PagedList<Submission>.CreateAsync(query, page, 12);
        ViewBag.Liked = uid is null
            ? new HashSet<int>()
            : (await _db.PaintingLikes.Where(l => l.UserId == uid).Select(l => l.SubmissionId).ToListAsync()).ToHashSet();
        ViewBag.Competitions = new SelectList(
            await _db.Competitions.Where(c => c.Submissions.Any(s => s.Grade == SubmissionGrade.Best)).OrderByDescending(c => c.StartDate).ToListAsync(),
            "CompetitionId", "Title", competitionId);
        ViewBag.Q = q; ViewBag.Sort = sort; ViewBag.CompetitionId = competitionId;
        return View(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var s = await _db.Submissions.AsNoTracking().Include(x => x.Student).Include(x => x.Competition)
            .Include(x => x.Evaluation).Include(x => x.Likes)
            .FirstOrDefaultAsync(x => x.SubmissionId == id && x.Grade == SubmissionGrade.Best);
        if (s is null) return NotFound();

        var uid = User.Identity?.IsAuthenticated == true ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
        ViewBag.HasLiked = uid != null && s.Likes.Any(l => l.UserId == uid);
        ViewBag.Award = await _db.Awards.AsNoTracking().Include(a => a.CompetitionAward).FirstOrDefaultAsync(a => a.SubmissionId == id);
        ViewBag.More = await _db.Submissions.AsNoTracking().Include(x => x.Student)
            .Where(x => x.Grade == SubmissionGrade.Best && x.SubmissionId != id && x.CompetitionId == s.CompetitionId)
            .OrderByDescending(x => x.SubmittedAt).Take(4).ToListAsync();
        return View(s);
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Like(int id)
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        if (!await _db.Submissions.AnyAsync(s => s.SubmissionId == id && s.Grade == SubmissionGrade.Best)) return NotFound();

        var existing = await _db.PaintingLikes.FirstOrDefaultAsync(l => l.SubmissionId == id && l.UserId == uid);
        var liked = existing is null;
        if (existing is null) _db.PaintingLikes.Add(new PaintingLike { SubmissionId = id, UserId = uid });
        else _db.PaintingLikes.Remove(existing);
        await _db.SaveChangesAsync();

        return Json(new { liked, count = await _db.PaintingLikes.CountAsync(l => l.SubmissionId == id) });
    }
}
