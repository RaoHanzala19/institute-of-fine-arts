using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Infrastructure;
using InstituteOfFineArts.Services;
using InstituteOfFineArts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Controllers;

public class SubmissionDetailsVm
{
    public Submission Submission { get; set; } = null!;
    public Award? Award { get; set; }
    public List<ExhibitionListing> Listings { get; set; } = new();
    public int Likes { get; set; }
}

[Authorize(Roles = Roles.Internal)]
public class SubmissionsController : PortalController
{
    private readonly NotificationService _notify;

    public SubmissionsController(AppDbContext db, NotificationService notify) : base(db) => _notify = notify;

    public async Task<IActionResult> Index(int? competitionId, SubmissionGrade? grade, string? q, bool late = false,
        int page = 1, bool export = false)
    {
        var query = Db.Submissions.AsNoTracking().Include(s => s.Student).Include(s => s.Competition).AsQueryable();
        if (competitionId.HasValue) query = query.Where(s => s.CompetitionId == competitionId);
        if (grade.HasValue) query = query.Where(s => s.Grade == grade);
        if (late) query = query.Where(s => s.IsLateSubmission);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.PaintingTitle.Contains(q) || s.Student.FirstName.Contains(q) ||
                                     s.Student.LastName.Contains(q) || s.Student.EnrollmentNumber.Contains(q));

        query = query.OrderByDescending(s => s.SubmittedAt);

        if (export)
        {
            var all = await query.ToListAsync();
            return Csv.File("submissions",
                new[] { "Painting", "Student", "Enrollment", "Competition", "Submitted", "Late", "Grade" },
                all.Select(s => new object?[] { s.PaintingTitle, s.Student.FullName, s.Student.EnrollmentNumber, s.Competition.Title, s.SubmittedAt, s.IsLateSubmission ? "Yes" : "No", s.Grade }));
        }

        ViewBag.Competitions = new SelectList(await Db.Competitions.OrderByDescending(c => c.StartDate).ToListAsync(),
            "CompetitionId", "Title", competitionId);
        ViewBag.Q = q;
        ViewBag.Grade = grade;
        ViewBag.Late = late;
        ViewBag.CompetitionId = competitionId;
        return View(await PagedList<Submission>.CreateAsync(query, page, 12));
    }

    public async Task<IActionResult> Details(int id)
    {
        var s = await Db.Submissions.AsNoTracking()
            .Include(x => x.Student).Include(x => x.Competition)
            .Include(x => x.Evaluation).ThenInclude(e => e!.EvaluatedBy)
            .FirstOrDefaultAsync(x => x.SubmissionId == id);
        if (s is null) return NotFound();

        return View(new SubmissionDetailsVm
        {
            Submission = s,
            Award = await Db.Awards.AsNoTracking().Include(a => a.CompetitionAward).FirstOrDefaultAsync(a => a.SubmissionId == id),
            Listings = await Db.ExhibitionListings.AsNoTracking().Include(l => l.Exhibition).Where(l => l.SubmissionId == id).ToListAsync(),
            Likes = await Db.PaintingLikes.CountAsync(l => l.SubmissionId == id)
        });
    }

    [Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Evaluate(int id)
    {
        var s = await Db.Submissions.AsNoTracking().Include(x => x.Student).Include(x => x.Competition)
            .Include(x => x.Evaluation).FirstOrDefaultAsync(x => x.SubmissionId == id);
        if (s is null) return NotFound();

        ViewBag.Submission = s;
        return View(new EvaluateVm
        {
            SubmissionId = id,
            Grade = s.Evaluation?.Grade ?? (s.IsLateSubmission ? SubmissionGrade.Disqualified : SubmissionGrade.Good),
            PositivePoints = s.Evaluation?.PositivePoints,
            NegativePoints = s.Evaluation?.NegativePoints,
            AreasOfImprovement = s.Evaluation?.AreasOfImprovement,
            GeneralRemarks = s.Evaluation?.GeneralRemarks
        });
    }

    [HttpPost, Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Evaluate(int id, EvaluateVm vm)
    {
        var s = await Db.Submissions.Include(x => x.Student).Include(x => x.Competition)
            .Include(x => x.Evaluation).FirstOrDefaultAsync(x => x.SubmissionId == id);
        if (s is null || id != vm.SubmissionId) return NotFound();

        if (s.IsLateSubmission && vm.Grade != SubmissionGrade.Disqualified)
            ModelState.AddModelError(nameof(vm.Grade), "This entry was submitted after the deadline and can only be disqualified.");
        if (vm.Grade == SubmissionGrade.Pending)
            ModelState.AddModelError(nameof(vm.Grade), "Choose a final grade for the painting.");
        if (vm.Grade == SubmissionGrade.Disqualified && string.IsNullOrWhiteSpace(vm.GeneralRemarks))
            ModelState.AddModelError(nameof(vm.GeneralRemarks), "Please explain why the entry is disqualified.");

        if (!ModelState.IsValid)
        {
            ViewBag.Submission = s;
            return View(vm);
        }

        var previous = s.Grade;
        s.Grade = vm.Grade;

        if (s.Evaluation is null)
        {
            s.Evaluation = new Evaluation { EvaluatedById = UserId };
        }
        else
        {
            s.Evaluation.UpdatedAt = DateTime.Now;
            s.Evaluation.EvaluatedById = UserId;
        }
        s.Evaluation.Grade = vm.Grade;
        s.Evaluation.PositivePoints = vm.PositivePoints;
        s.Evaluation.NegativePoints = vm.NegativePoints;
        s.Evaluation.AreasOfImprovement = vm.AreasOfImprovement;
        s.Evaluation.GeneralRemarks = vm.GeneralRemarks;

        _notify.Notify(s.Student.ApplicationUserId,
            vm.Grade == SubmissionGrade.Disqualified
                ? $"“{s.PaintingTitle}” was disqualified"
                : $"“{s.PaintingTitle}” was graded {vm.Grade.DisplayName()}",
            "Open your feedback to see the detailed remarks from the evaluator.",
            Url.Action("SubmissionDetails", "Student", new { id = s.SubmissionId }),
            vm.Grade switch
            {
                SubmissionGrade.Best or SubmissionGrade.Better => NotificationType.Success,
                SubmissionGrade.Disqualified => NotificationType.Warning,
                _ => NotificationType.Info
            });

        await Db.SaveChangesAsync();

        Success(previous == SubmissionGrade.Pending ? "Evaluation saved." : "Evaluation updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Quick review queue: next pending painting that still needs a grade.</summary>
    [Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Next()
    {
        var id = await Db.Submissions.Where(s => s.Grade == SubmissionGrade.Pending)
            .OrderBy(s => s.SubmittedAt).Select(s => (int?)s.SubmissionId).FirstOrDefaultAsync();
        if (id is null)
        {
            Success("Nothing left to review — the queue is empty.");
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Evaluate), new { id });
    }
}
