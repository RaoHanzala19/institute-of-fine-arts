using System.Globalization;
using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Infrastructure;
using InstituteOfFineArts.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Controllers;

public class DashboardController : PortalController
{
    public DashboardController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole(Roles.Admin)) return View("Admin", await AdminAsync());
        if (User.IsInRole(Roles.Manager)) return View("Manager", await ManagerAsync());
        if (User.IsInRole(Roles.Staff)) return View("Staff", await StaffAsync());
        if (User.IsInRole(Roles.Student)) return View("Student", await StudentAsync());
        return RedirectToAction("Index", "Home");
    }

    private IQueryable<Announcement> ActiveAnnouncements(params AnnouncementAudience[] audiences)
    {
        var now = DateTime.Now;
        return Db.Announcements.AsNoTracking().Include(a => a.CreatedBy)
            .Where(a => (a.ExpiresAt == null || a.ExpiresAt >= now) && audiences.Contains(a.Audience))
            .OrderByDescending(a => a.IsPinned).ThenByDescending(a => a.PublishedAt);
    }

    private async Task<ChartData> GradeMixAsync(IQueryable<Submission> source)
    {
        var groups = await source.GroupBy(s => s.Grade).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
        var chart = new ChartData();
        foreach (var g in Enum.GetValues<SubmissionGrade>())
        {
            chart.Labels.Add(g.DisplayName());
            chart.Values.Add(groups.FirstOrDefault(x => x.Key == g)?.Count ?? 0);
        }
        return chart;
    }

    // ── Admin ──────────────────────────────────────────────────────────────────
    private async Task<AdminDashboardVm> AdminAsync()
    {
        var since = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
        var perMonth = await Db.Submissions.Where(s => s.SubmittedAt >= since)
            .GroupBy(s => new { s.SubmittedAt.Year, s.SubmittedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();

        var months = new ChartData();
        for (var i = 0; i < 6; i++)
        {
            var m = since.AddMonths(i);
            months.Labels.Add(m.ToString("MMM", CultureInfo.InvariantCulture));
            months.Values.Add(perMonth.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Count ?? 0);
        }

        var roles = await (from ur in Db.UserRoles join r in Db.Roles on ur.RoleId equals r.Id
                           group ur by r.Name into g select new { Role = g.Key, Count = g.Count() }).ToListAsync();

        return new AdminDashboardVm
        {
            Students = await Db.Students.CountAsync(s => s.IsActive),
            Staff = await Db.Staff.CountAsync(s => s.IsActive),
            Managers = roles.FirstOrDefault(r => r.Role == Roles.Manager)?.Count ?? 0,
            ActiveCompetitions = await Db.Competitions.CountAsync(c => c.Status != CompetitionStatus.Concluded),
            Submissions = await Db.Submissions.CountAsync(),
            PendingReviews = await Db.Submissions.CountAsync(s => s.Grade == SubmissionGrade.Pending),
            UnreadMessages = await Db.ContactMessages.CountAsync(m => !m.IsRead),
            SubmissionsPerMonth = months,
            GradeMix = await GradeMixAsync(Db.Submissions),
            UsersByRole = new ChartData
            {
                Labels = roles.Select(r => r.Role ?? "").ToList(),
                Values = roles.Select(r => (decimal)r.Count).ToList()
            },
            Recent = await Db.AuditLogs.AsNoTracking().OrderByDescending(a => a.Timestamp).Take(8).ToListAsync(),
            Announcements = await ActiveAnnouncements(AnnouncementAudience.Everyone, AnnouncementAudience.Staff, AnnouncementAudience.Students).Take(3).ToListAsync()
        };
    }

    // ── Manager ────────────────────────────────────────────────────────────────
    private async Task<ManagerDashboardVm> ManagerAsync()
    {
        var sold = Db.ExhibitionListings.Where(l => l.SaleStatus == SaleStatus.Sold);

        var byExhibition = await sold.GroupBy(l => l.Exhibition.ExhibitionName)
            .Select(g => new { Name = g.Key, Total = g.Sum(l => l.FinalSalePrice ?? 0) })
            .OrderByDescending(x => x.Total).Take(6).ToListAsync();

        var perCompetition = await Db.Competitions.OrderByDescending(c => c.StartDate).Take(6)
            .Select(c => new { c.Title, Count = c.Submissions.Count }).ToListAsync();

        return new ManagerDashboardVm
        {
            Students = await Db.Students.CountAsync(),
            Staff = await Db.Staff.CountAsync(),
            Competitions = await Db.Competitions.CountAsync(),
            Submissions = await Db.Submissions.CountAsync(),
            Awards = await Db.Awards.CountAsync(),
            Exhibitions = await Db.Exhibitions.CountAsync(),
            Listings = await Db.ExhibitionListings.CountAsync(),
            PaintingsSold = await sold.CountAsync(),
            GrossSales = await sold.SumAsync(l => l.FinalSalePrice ?? 0),
            Commission = await sold.SumAsync(l => l.CommissionAmount ?? 0),
            PayoutsDue = await sold.SumAsync(l => (l.NetPayableToStudent ?? 0) - (l.AmountPaidToStudent ?? 0)),
            PayoutsPendingCount = await Db.ExhibitionListings.CountAsync(l => l.PaymentStatus == PaymentStatus.Pending),
            SalesByExhibition = new ChartData
            {
                Labels = byExhibition.Select(x => x.Name.Length > 22 ? x.Name[..21] + "…" : x.Name).ToList(),
                Values = byExhibition.Select(x => x.Total).ToList()
            },
            SubmissionsPerCompetition = new ChartData
            {
                Labels = perCompetition.Select(x => x.Title.Length > 22 ? x.Title[..21] + "…" : x.Title).ToList(),
                Values = perCompetition.Select(x => (decimal)x.Count).ToList()
            },
            RecentSales = await sold.AsNoTracking().Include(l => l.Exhibition).Include(l => l.Submission).ThenInclude(s => s.Student)
                .OrderByDescending(l => l.DateSold).Take(6).ToListAsync(),
            UpcomingExhibitions = await Db.Exhibitions.AsNoTracking()
                .Where(e => e.Status == ExhibitionStatus.Upcoming || e.Status == ExhibitionStatus.Ongoing)
                .OrderBy(e => e.ExhibitionStartDate).Take(4).ToListAsync(),
            Announcements = await ActiveAnnouncements(AnnouncementAudience.Everyone, AnnouncementAudience.Staff).Take(3).ToListAsync()
        };
    }

    // ── Staff ──────────────────────────────────────────────────────────────────
    private async Task<StaffDashboardVm> StaffAsync()
    {
        var name = User.FindFirst("full_name")?.Value ?? User.Identity?.Name ?? "";
        var now = DateTime.Now;

        var concluded = await Db.Competitions.AsNoTracking()
            .Where(c => c.Status == CompetitionStatus.Concluded && c.CompetitionAwards.Any())
            .Select(c => new { Competition = c, Tiers = c.CompetitionAwards.Count, Issued = c.IssuedAwards.Count })
            .ToListAsync();

        return new StaffDashboardVm
        {
            Name = name,
            TotalCompetitions = await Db.Competitions.CountAsync(),
            Ongoing = await Db.Competitions.CountAsync(c => c.Status == CompetitionStatus.Ongoing),
            Submissions = await Db.Submissions.CountAsync(),
            Pending = await Db.Submissions.CountAsync(s => s.Grade == SubmissionGrade.Pending),
            Exhibitions = await Db.Exhibitions.CountAsync(),
            AwardsIssued = await Db.Awards.CountAsync(),
            Queue = await Db.Submissions.AsNoTracking().Include(s => s.Student).Include(s => s.Competition)
                .Where(s => s.Grade == SubmissionGrade.Pending)
                .OrderBy(s => s.SubmittedAt).Take(6).ToListAsync(),
            PastDeadline = await Db.Competitions.AsNoTracking()
                .Where(c => c.Status != CompetitionStatus.Concluded && c.EndDate < now)
                .OrderBy(c => c.EndDate).Take(4).ToListAsync(),
            AwaitingAwards = concluded.Where(x => x.Issued < x.Tiers).Select(x => x.Competition).Take(4).ToList(),
            GradeMix = await GradeMixAsync(Db.Submissions),
            Announcements = await ActiveAnnouncements(AnnouncementAudience.Everyone, AnnouncementAudience.Staff).Take(3).ToListAsync()
        };
    }

    // ── Student ────────────────────────────────────────────────────────────────
    private async Task<StudentDashboardVm> StudentAsync()
    {
        var student = await Db.Students.AsNoTracking().FirstAsync(s => s.ApplicationUserId == UserId);
        var mine = Db.Submissions.Where(s => s.StudentId == student.StudentId);
        var submittedIds = await mine.Select(s => s.CompetitionId).ToListAsync();
        var listings = await Db.ExhibitionListings.AsNoTracking()
            .Include(l => l.Exhibition).Include(l => l.Submission)
            .Where(l => l.Submission.StudentId == student.StudentId)
            .OrderByDescending(l => l.ListedAt).ToListAsync();

        return new StudentDashboardVm
        {
            Student = student,
            Open = await Db.Competitions.AsNoTracking().Include(c => c.CompetitionAwards)
                .Where(c => c.Status != CompetitionStatus.Concluded && !submittedIds.Contains(c.CompetitionId))
                .OrderBy(c => c.EndDate).Take(4).ToListAsync(),
            Submissions = await mine.AsNoTracking().Include(s => s.Competition).Include(s => s.Evaluation)
                .OrderByDescending(s => s.SubmittedAt).Take(5).ToListAsync(),
            Awards = await Db.Awards.AsNoTracking().Include(a => a.Competition).Include(a => a.CompetitionAward)
                .Where(a => a.StudentId == student.StudentId).OrderByDescending(a => a.DateIssued).Take(4).ToListAsync(),
            Listings = listings,
            Inspiration = await Db.Submissions.AsNoTracking().Include(s => s.Student).Include(s => s.Competition)
                .Where(s => s.Grade == SubmissionGrade.Best).OrderByDescending(s => s.SubmittedAt).Take(4).ToListAsync(),
            Announcements = await ActiveAnnouncements(AnnouncementAudience.Everyone, AnnouncementAudience.Students).Take(3).ToListAsync(),
            Evaluated = await mine.CountAsync(s => s.Grade != SubmissionGrade.Pending),
            Pending = await mine.CountAsync(s => s.Grade == SubmissionGrade.Pending),
            Earned = listings.Where(l => l.SaleStatus == SaleStatus.Sold).Sum(l => l.AmountPaidToStudent ?? 0),
            GradeMix = await GradeMixAsync(mine)
        };
    }
}
