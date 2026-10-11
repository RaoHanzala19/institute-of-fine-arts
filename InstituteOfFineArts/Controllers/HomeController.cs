using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var vm = new HomeVm
        {
            Featured = await _db.Competitions.Include(c => c.CompetitionAwards)
                .Where(c => c.Status == CompetitionStatus.Ongoing && c.EndDate >= now)
                .OrderBy(c => c.EndDate).FirstOrDefaultAsync(),
            Upcoming = await _db.Competitions.AsNoTracking()
                .Where(c => c.Status == CompetitionStatus.Upcoming)
                .OrderBy(c => c.StartDate).Take(3).ToListAsync(),
            Paintings = await _db.Submissions.AsNoTracking()
                .Include(s => s.Student).Include(s => s.Competition).Include(s => s.Likes)
                .Where(s => s.Grade == SubmissionGrade.Best)
                .OrderByDescending(s => s.SubmittedAt).Take(8).ToListAsync(),
            RecentAwards = await _db.Awards.AsNoTracking()
                .Include(a => a.Student).Include(a => a.Competition).Include(a => a.CompetitionAward)
                .Include(a => a.WinningSubmission)
                .OrderByDescending(a => a.DateIssued).Take(4).ToListAsync(),
            Announcements = await _db.Announcements.AsNoTracking()
                .Where(a => a.Audience == AnnouncementAudience.Everyone && (a.ExpiresAt == null || a.ExpiresAt >= now))
                .OrderByDescending(a => a.IsPinned).ThenByDescending(a => a.PublishedAt).Take(3).ToListAsync(),
            Students = await _db.Students.CountAsync(s => s.IsActive),
            Paintings_Count = await _db.Submissions.CountAsync(s => s.Grade != SubmissionGrade.Disqualified),
            Competitions = await _db.Competitions.CountAsync(),
            Awards = await _db.Awards.CountAsync(),
            Exhibitions = await _db.Exhibitions.CountAsync(e => e.Status != ExhibitionStatus.Cancelled)
        };
        return View(vm);
    }

    public IActionResult About() => View();

    public IActionResult FAQ() => View();

    [HttpGet]
    public IActionResult Contact() => View(new ContactVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactVm model)
    {
        if (!ModelState.IsValid) return View(model);

        _db.ContactMessages.Add(new ContactMessage
        {
            Name = model.Name.Trim(), Email = model.Email.Trim(),
            Subject = model.Subject.Trim(), Message = model.Message.Trim()
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Thank you — your message has reached our front desk. We will reply by email soon.";
        return RedirectToAction(nameof(Contact));
    }

    [Route("Home/Status")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        ViewData["Code"] = code;
        return View("Status");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}

