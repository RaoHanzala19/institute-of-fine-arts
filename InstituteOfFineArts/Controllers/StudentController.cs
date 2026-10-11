using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Infrastructure;
using InstituteOfFineArts.Services;
using InstituteOfFineArts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Controllers;

public class StudentCompetitionVm
{
    public Competition Competition { get; set; } = null!;
    public Submission? Mine { get; set; }
}

/// <summary>Everything a signed-in student can do: browse competitions, upload work, follow grades, awards and sales.</summary>
[Authorize(Roles = Roles.Student)]
public class StudentController : PortalController
{
    private readonly IImageService _images;
    private readonly NotificationService _notify;

    public StudentController(AppDbContext db, IImageService images, NotificationService notify) : base(db)
    {
        _images = images;
        _notify = notify;
    }

    private Task<Student?> CurrentAsync() =>
        Db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == UserId);

    // ── Competitions ───────────────────────────────────────────────────────────
    public async Task<IActionResult> Competitions(string tab = "open", string? q = null)
    {
        var me = await CurrentAsync();
        if (me is null) return Forbid();

        var query = Db.Competitions.AsNoTracking().Include(c => c.CompetitionAwards).AsQueryable();
        query = tab == "past"
            ? query.Where(c => c.Status == CompetitionStatus.Concluded).OrderByDescending(c => c.EndDate)
            : query.Where(c => c.Status != CompetitionStatus.Concluded).OrderBy(c => c.EndDate);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(c => c.Title.Contains(q) || (c.Theme != null && c.Theme.Contains(q)));

        var mine = await Db.Submissions.AsNoTracking().Where(s => s.StudentId == me.StudentId).ToListAsync();
        var rows = (await query.ToListAsync())
            .Select(c => new StudentCompetitionVm { Competition = c, Mine = mine.FirstOrDefault(s => s.CompetitionId == c.CompetitionId) })
            .ToList();

        ViewBag.Tab = tab; ViewBag.Q = q;
        return View(rows);
    }

    public async Task<IActionResult> Details(int id)
    {
        var me = await CurrentAsync();
        if (me is null) return Forbid();

        var c = await Db.Competitions.AsNoTracking().Include(x => x.CompetitionAwards).FirstOrDefaultAsync(x => x.CompetitionId == id);
        if (c is null) return NotFound();

        ViewBag.Winners = await Db.Awards.AsNoTracking().Include(a => a.Student).Include(a => a.CompetitionAward).Include(a => a.WinningSubmission)
            .Where(a => a.CompetitionId == id).OrderBy(a => a.CompetitionAward!.Rank).ToListAsync();
        return View(new StudentCompetitionVm
        {
            Competition = c,
            Mine = await Db.Submissions.AsNoTracking().Include(s => s.Evaluation)
                .FirstOrDefaultAsync(s => s.StudentId == me.StudentId && s.CompetitionId == id)
        });
    }

    // ── Upload ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Upload(int? competitionId)
    {
        var me = await CurrentAsync();
        if (me is null) return Forbid();

        var open = await OpenCompetitionsAsync(me.StudentId);
        if (open.Count == 0)
        {
            Warn("There is no competition open for a new entry right now. You may already have entered all of them.");
            return RedirectToAction(nameof(Competitions));
        }

        ViewBag.Competitions = open;
        return View("SubmissionForm", new SubmissionFormVm { CompetitionId = competitionId ?? open[0].CompetitionId });
    }

    [HttpPost]
    public async Task<IActionResult> Upload(SubmissionFormVm vm)
    {
        var me = await CurrentAsync();
        if (me is null) return Forbid();

        var competition = await Db.Competitions.FirstOrDefaultAsync(c => c.CompetitionId == vm.CompetitionId);
        if (competition is null) ModelState.AddModelError(nameof(vm.CompetitionId), "Competition not found.");
        else if (!competition.AcceptsSubmissions)
            ModelState.AddModelError(nameof(vm.CompetitionId), competition.Status == CompetitionStatus.Concluded
                ? "This competition has concluded and no longer accepts entries."
                : "This competition has not opened yet.");
        else if (await Db.Submissions.AnyAsync(s => s.StudentId == me.StudentId && s.CompetitionId == vm.CompetitionId))
            ModelState.AddModelError(nameof(vm.CompetitionId), "You have already entered this competition — edit your existing entry instead.");

        var late = competition is { DeadlinePassed: true };
        if (late && !vm.AcceptLate)
            ModelState.AddModelError(nameof(vm.AcceptLate), "The deadline has passed. Tick the box to confirm you understand this entry will be disqualified.");

        if (vm.PaintingFile is null || vm.PaintingFile.Length == 0)
            ModelState.AddModelError(nameof(vm.PaintingFile), "Please choose your painting (JPEG).");

        SavedImage? image = null;
        if (ModelState.IsValid)
        {
            var (saved, error) = await _images.SaveJpegAsync(vm.PaintingFile!);
            if (error != null) ModelState.AddModelError(nameof(vm.PaintingFile), error);
            image = saved;
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Competitions = await OpenCompetitionsAsync(me.StudentId);
            return View("SubmissionForm", vm);
        }

        var submission = new Submission
        {
            StudentId = me.StudentId, CompetitionId = vm.CompetitionId, PaintingTitle = vm.PaintingTitle.Trim(),
            ImageFilePath = image!.Path, ThumbnailPath = image.ThumbPath, OriginalFileName = Path.GetFileName(vm.PaintingFile!.FileName),
            FileSizeInBytes = image.Size, ImageWidth = image.Width, ImageHeight = image.Height,
            AccompanyingText = vm.AccompanyingText.Trim(), TextType = vm.TextType, CreativeDescription = vm.CreativeDescription,
            SubmittedAt = DateTime.Now, IsLateSubmission = late,
            Grade = late ? SubmissionGrade.Disqualified : SubmissionGrade.Pending
        };
        Db.Submissions.Add(submission);
        _notify.Notify(competition!.CreatedById, "New painting submitted",
            $"{me.FullName} entered “{submission.PaintingTitle}” in {competition.Title}.",
            Url.Action("Index", "Submissions", new { competitionId = competition.CompetitionId }));
        await Db.SaveChangesAsync();

        if (late) Warn("Your painting was uploaded after the deadline and has been marked as disqualified.");
        else Success("Your masterpiece is in! You will be notified when it has been evaluated.");
        return RedirectToAction(nameof(SubmissionDetails), new { id = submission.SubmissionId });
    }

    // ── Edit / delete (only before the deadline) ───────────────────────────────
    public async Task<IActionResult> Edit(int id)
    {
        var (s, redirect) = await LoadEditableAsync(id);
        if (redirect != null) return redirect;

        ViewBag.Competitions = new List<Competition> { s!.Competition };
        return View("SubmissionForm", new SubmissionFormVm
        {
            SubmissionId = s.SubmissionId, CompetitionId = s.CompetitionId, PaintingTitle = s.PaintingTitle,
            TextType = s.TextType, AccompanyingText = s.AccompanyingText, CreativeDescription = s.CreativeDescription,
            ExistingImagePath = s.ThumbnailPath ?? s.ImageFilePath
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, SubmissionFormVm vm)
    {
        var (s, redirect) = await LoadEditableAsync(id);
        if (redirect != null) return redirect;

        vm.CompetitionId = s!.CompetitionId;
        ModelState.Remove(nameof(vm.CompetitionId));
        SavedImage? image = null;

        if (vm.PaintingFile is { Length: > 0 })
        {
            var (saved, error) = await _images.SaveJpegAsync(vm.PaintingFile);
            if (error != null) ModelState.AddModelError(nameof(vm.PaintingFile), error);
            image = saved;
        }

        if (!ModelState.IsValid)
        {
            vm.ExistingImagePath = s.ThumbnailPath ?? s.ImageFilePath;
            ViewBag.Competitions = new List<Competition> { s.Competition };
            return View("SubmissionForm", vm);
        }

        s.PaintingTitle = vm.PaintingTitle.Trim();
        s.TextType = vm.TextType;
        s.AccompanyingText = vm.AccompanyingText.Trim();
        s.CreativeDescription = vm.CreativeDescription;
        s.LastUpdatedAt = DateTime.Now;

        if (image != null)
        {
            _images.Delete(s.ImageFilePath, s.ThumbnailPath);
            s.ImageFilePath = image.Path; s.ThumbnailPath = image.ThumbPath;
            s.OriginalFileName = Path.GetFileName(vm.PaintingFile!.FileName);
            s.FileSizeInBytes = image.Size; s.ImageWidth = image.Width; s.ImageHeight = image.Height;
        }

        await Db.SaveChangesAsync();
        Success("Your entry has been updated.");
        return RedirectToAction(nameof(SubmissionDetails), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var (s, redirect) = await LoadEditableAsync(id);
        if (redirect != null) return redirect;

        if (await Db.ExhibitionListings.AnyAsync(l => l.SubmissionId == id) || await Db.Awards.AnyAsync(a => a.SubmissionId == id))
        {
            Error("This painting has an award or an exhibition listing, so it cannot be withdrawn.");
            return RedirectToAction(nameof(SubmissionDetails), new { id });
        }

        _images.Delete(s!.ImageFilePath, s.ThumbnailPath);
        Db.Submissions.Remove(s);
        await Db.SaveChangesAsync();
        Success("Your entry was withdrawn.");
        return RedirectToAction(nameof(Submissions));
    }

    // ── My work ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Submissions()
    {
        var me = await CurrentAsync();
        if (me is null) return Forbid();
        return View(await Db.Submissions.AsNoTracking().Include(s => s.Competition).Include(s => s.Evaluation)
            .Where(s => s.StudentId == me.StudentId).OrderByDescending(s => s.SubmittedAt).ToListAsync());
    }

    public async Task<IActionResult> SubmissionDetails(int id)
    {
        var me = await CurrentAsync();
        if (me is null) return Forbid();

        var s = await Db.Submissions.AsNoTracking().Include(x => x.Competition).Include(x => x.Evaluation).ThenInclude(e => e!.EvaluatedBy)
            .Include(x => x.Likes).FirstOrDefaultAsync(x => x.SubmissionId == id && x.StudentId == me.StudentId);
        if (s is null) return NotFound();

        return View(new SubmissionDetailsVm
        {
            Submission = s, Likes = s.Likes.Count,
            Award = await Db.Awards.AsNoTracking().Include(a => a.CompetitionAward).FirstOrDefaultAsync(a => a.SubmissionId == id),
            Listings = await Db.ExhibitionListings.AsNoTracking().Include(l => l.Exhibition).Where(l => l.SubmissionId == id).ToListAsync()
        });
    }

    public async Task<IActionResult> Feedback()
    {
        var me = await CurrentAsync();
        if (me is null) return Forbid();
        return View(await Db.Submissions.AsNoTracking().Include(s => s.Competition).Include(s => s.Evaluation).ThenInclude(e => e!.EvaluatedBy)
            .Where(s => s.StudentId == me.StudentId && s.Evaluation != null)
            .OrderByDescending(s => s.Evaluation!.EvaluatedAt).ToListAsync());
    }

    public async Task<IActionResult> Awards()
    {
        var me = await CurrentAsync();
        if (me is null) return Forbid();
        return View(await Db.Awards.AsNoTracking().Include(a => a.Competition).Include(a => a.CompetitionAward).Include(a => a.WinningSubmission)
            .Where(a => a.StudentId == me.StudentId).OrderByDescending(a => a.DateIssued).ToListAsync());
    }

    public async Task<IActionResult> Exhibitions()
    {
        var me = await CurrentAsync();
        if (me is null) return Forbid();
        return View(await Db.ExhibitionListings.AsNoTracking().Include(l => l.Exhibition).Include(l => l.Submission)
            .Where(l => l.Submission.StudentId == me.StudentId).OrderByDescending(l => l.ListedAt).ToListAsync());
    }

    // ── helpers ────────────────────────────────────────────────────────────────
    private async Task<List<Competition>> OpenCompetitionsAsync(int studentId)
    {
        var taken = await Db.Submissions.Where(s => s.StudentId == studentId).Select(s => s.CompetitionId).ToListAsync();
        var now = DateTime.Now;
        return await Db.Competitions.AsNoTracking()
            .Where(c => c.Status != CompetitionStatus.Concluded && c.StartDate <= now && !taken.Contains(c.CompetitionId))
            .OrderBy(c => c.EndDate).ToListAsync();
    }

    private async Task<(Submission?, IActionResult?)> LoadEditableAsync(int id)
    {
        var me = await CurrentAsync();
        if (me is null) return (null, Forbid());

        var s = await Db.Submissions.Include(x => x.Competition)
            .FirstOrDefaultAsync(x => x.SubmissionId == id && x.StudentId == me.StudentId);
        if (s is null) return (null, NotFound());

        if (s.Competition.DeadlinePassed || s.Competition.Status == CompetitionStatus.Concluded)
        {
            Error("The deadline has passed — this entry can no longer be changed or withdrawn.");
            return (null, RedirectToAction(nameof(SubmissionDetails), new { id }));
        }
        return (s, null);
    }
}
