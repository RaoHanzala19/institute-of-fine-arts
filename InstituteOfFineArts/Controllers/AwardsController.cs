using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Infrastructure;
using InstituteOfFineArts.Services;
using InstituteOfFineArts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Controllers;

[Authorize(Roles = Roles.Internal)]
public class AwardsController : PortalController
{
    private readonly NotificationService _notify;

    public AwardsController(AppDbContext db, NotificationService notify) : base(db) => _notify = notify;

    public async Task<IActionResult> Index(string? q, int? competitionId, int page = 1, bool export = false)
    {
        var query = Db.Awards.AsNoTracking().Include(a => a.Student).Include(a => a.Competition)
            .Include(a => a.CompetitionAward).Include(a => a.WinningSubmission).AsQueryable();
        if (competitionId.HasValue) query = query.Where(a => a.CompetitionId == competitionId);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(a => a.Student.FirstName.Contains(q) || a.Student.LastName.Contains(q) ||
                                     a.Competition.Title.Contains(q) || a.CertificateNumber.Contains(q));
        query = query.OrderByDescending(a => a.DateIssued).ThenByDescending(a => a.AwardId);

        if (export)
        {
            var all = await query.ToListAsync();
            return Csv.File("awards",
                new[] { "Certificate", "Student", "Competition", "Award", "Prize money", "Date issued" },
                all.Select(a => new object?[] { a.CertificateNumber, a.Student.FullName, a.Competition.Title, a.DisplayTitle, a.CompetitionAward?.PrizeMoney, a.DateIssued }));
        }

        ViewBag.Q = q;
        ViewBag.CompetitionId = competitionId;
        ViewBag.Competitions = new SelectList(await Db.Competitions.OrderByDescending(c => c.StartDate).ToListAsync(),
            "CompetitionId", "Title", competitionId);
        return View(await PagedList<Award>.CreateAsync(query, page, 12));
    }

    public async Task<IActionResult> Details(int id)
    {
        var a = await Db.Awards.AsNoTracking().Include(x => x.Student).Include(x => x.Competition)
            .Include(x => x.CompetitionAward).Include(x => x.WinningSubmission).Include(x => x.IssuedBy)
            .FirstOrDefaultAsync(x => x.AwardId == id);
        return a is null ? NotFound() : View(a);
    }

    // ── Issue ─────────────────────────────────────────────────────────────────
    [Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Create(int? competitionId, int? submissionId)
    {
        var vm = new AwardFormVm();
        if (submissionId.HasValue)
        {
            var s = await Db.Submissions.AsNoTracking().FirstOrDefaultAsync(x => x.SubmissionId == submissionId);
            if (s != null) { vm.SubmissionId = s.SubmissionId; vm.CompetitionId = s.CompetitionId; }
        }
        else if (competitionId.HasValue) vm.CompetitionId = competitionId.Value;

        await FillListsAsync(vm.CompetitionId);
        return View("Form", vm);
    }

    [HttpPost, Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Create(AwardFormVm vm)
    {
        await ValidateAsync(vm, isNew: true);
        if (!ModelState.IsValid)
        {
            await FillListsAsync(vm.CompetitionId);
            return View("Form", vm);
        }

        var sub = await Db.Submissions.Include(s => s.Student).FirstAsync(s => s.SubmissionId == vm.SubmissionId);
        var award = new Award
        {
            CertificateNumber = await NextCertificateNumberAsync(),
            StudentId = sub.StudentId,
            CompetitionId = sub.CompetitionId,
            SubmissionId = sub.SubmissionId,
            CompetitionAwardId = vm.CompetitionAwardId,
            CustomAwardTitle = vm.CustomAwardTitle,
            Remarks = vm.Remarks,
            DateIssued = vm.DateIssued!.Value,
            IssuedById = UserId
        };
        Db.Awards.Add(award);
        await Db.SaveChangesAsync();

        var title = await Db.Awards.Where(a => a.AwardId == award.AwardId)
            .Select(a => a.CustomAwardTitle ?? a.CompetitionAward!.AwardTitle ?? "Award of Excellence").FirstAsync();
        _notify.Notify(sub.Student.ApplicationUserId, $"🏆 You won: {title}",
            $"Congratulations! Your painting “{sub.PaintingTitle}” earned an award. Your certificate is ready.",
            Url.Action("Certificate", "Certificates", new { id = award.AwardId }), NotificationType.Success);
        await Db.SaveChangesAsync();

        Success($"Award issued. Certificate {award.CertificateNumber} is ready to print.");
        return RedirectToAction(nameof(Details), new { id = award.AwardId });
    }

    [Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Edit(int id)
    {
        var a = await Db.Awards.AsNoTracking().FirstOrDefaultAsync(x => x.AwardId == id);
        if (a is null) return NotFound();

        var vm = new AwardFormVm
        {
            AwardId = a.AwardId, CompetitionId = a.CompetitionId, SubmissionId = a.SubmissionId,
            CompetitionAwardId = a.CompetitionAwardId, CustomAwardTitle = a.CustomAwardTitle,
            Remarks = a.Remarks, DateIssued = a.DateIssued
        };
        await FillListsAsync(vm.CompetitionId, a.AwardId);
        return View("Form", vm);
    }

    [HttpPost, Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Edit(int id, AwardFormVm vm)
    {
        if (id != vm.AwardId) return BadRequest();
        var a = await Db.Awards.FirstOrDefaultAsync(x => x.AwardId == id);
        if (a is null) return NotFound();

        // competition and winner are fixed once issued
        vm.CompetitionId = a.CompetitionId;
        vm.SubmissionId = a.SubmissionId;
        ModelState.Remove(nameof(vm.CompetitionId));
        ModelState.Remove(nameof(vm.SubmissionId));
        await ValidateAsync(vm, isNew: false);

        if (!ModelState.IsValid)
        {
            await FillListsAsync(vm.CompetitionId, id);
            return View("Form", vm);
        }

        a.CompetitionAwardId = vm.CompetitionAwardId;
        a.CustomAwardTitle = vm.CustomAwardTitle;
        a.Remarks = vm.Remarks;
        a.DateIssued = vm.DateIssued!.Value;
        a.UpdatedAt = DateTime.Now;
        await Db.SaveChangesAsync();

        Success("Award updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await Db.Awards.FirstOrDefaultAsync(x => x.AwardId == id);
        if (a is null) return NotFound();
        Db.Awards.Remove(a);
        await Db.SaveChangesAsync();
        Success("Award revoked.");
        return RedirectToAction(nameof(Index));
    }

    // ── AJAX helpers for the issue form ───────────────────────────────────────
    [HttpGet, Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Candidates(int competitionId, int? awardId)
    {
        var awarded = await Db.Awards.Where(a => a.CompetitionId == competitionId && a.AwardId != awardId)
            .Select(a => a.SubmissionId).ToListAsync();

        var subRows = await Db.Submissions.AsNoTracking()
            .Where(s => s.CompetitionId == competitionId && s.Grade != SubmissionGrade.Disqualified && s.Grade != SubmissionGrade.Pending)
            .Where(s => !awarded.Contains(s.SubmissionId))
            .OrderBy(s => s.Grade)
            .Select(s => new { s.SubmissionId, s.PaintingTitle, s.Student.FirstName, s.Student.LastName, s.Grade })
            .ToListAsync();
        var subs = subRows.Select(s => new
        {
            id = s.SubmissionId,
            label = $"{s.PaintingTitle} — {s.FirstName} {s.LastName} ({s.Grade.DisplayName()})"
        });

        var takenTiers = await Db.Awards.Where(a => a.CompetitionId == competitionId && a.AwardId != awardId && a.CompetitionAwardId != null)
            .Select(a => a.CompetitionAwardId!.Value).ToListAsync();

        var tierRows = await Db.CompetitionAwards.AsNoTracking().Where(t => t.CompetitionId == competitionId)
            .OrderBy(t => t.Rank).ToListAsync();
        var tiers = tierRows.Select(t => new
        {
            id = t.CompetitionAwardId,
            label = t.AwardTitle + (t.PrizeMoney != null ? $" ({Ui.Money(t.PrizeMoney)})" : ""),
            taken = takenTiers.Contains(t.CompetitionAwardId)
        });

        return Json(new { submissions = subs, tiers });
    }

    // ── helpers ───────────────────────────────────────────────────────────────
    private async Task FillListsAsync(int competitionId, int? awardId = null)
    {
        ViewBag.Competitions = new SelectList(
            await Db.Competitions.AsNoTracking().Where(c => c.Status == CompetitionStatus.Concluded || c.CompetitionId == competitionId)
                .OrderByDescending(c => c.EndDate).ToListAsync(),
            "CompetitionId", "Title", competitionId);
        ViewBag.AwardId = awardId;
        ViewBag.HasConcluded = await Db.Competitions.AnyAsync(c => c.Status == CompetitionStatus.Concluded);
    }

    private async Task ValidateAsync(AwardFormVm vm, bool isNew)
    {
        var sub = await Db.Submissions.AsNoTracking().FirstOrDefaultAsync(s => s.SubmissionId == vm.SubmissionId);
        if (sub is null || sub.CompetitionId != vm.CompetitionId)
        {
            ModelState.AddModelError(nameof(vm.SubmissionId), "That painting does not belong to the chosen competition.");
            return;
        }
        if (sub.Grade == SubmissionGrade.Disqualified)
            ModelState.AddModelError(nameof(vm.SubmissionId), "Disqualified paintings cannot win awards.");
        if (sub.Grade == SubmissionGrade.Pending)
            ModelState.AddModelError(nameof(vm.SubmissionId), "Evaluate the painting before awarding it.");

        var comp = await Db.Competitions.AsNoTracking().FirstAsync(c => c.CompetitionId == vm.CompetitionId);
        if (isNew && comp.Status != CompetitionStatus.Concluded)
            ModelState.AddModelError(nameof(vm.CompetitionId), "Awards can only be issued once the competition is concluded.");

        if (isNew && await Db.Awards.AnyAsync(a => a.SubmissionId == vm.SubmissionId))
            ModelState.AddModelError(nameof(vm.SubmissionId), "This painting already has an award.");

        if (vm.CompetitionAwardId.HasValue)
        {
            var tierOk = await Db.CompetitionAwards.AnyAsync(t => t.CompetitionAwardId == vm.CompetitionAwardId && t.CompetitionId == vm.CompetitionId);
            if (!tierOk)
                ModelState.AddModelError(nameof(vm.CompetitionAwardId), "That award tier belongs to another competition.");
            else if (await Db.Awards.AnyAsync(a => a.CompetitionAwardId == vm.CompetitionAwardId && a.AwardId != vm.AwardId))
                ModelState.AddModelError(nameof(vm.CompetitionAwardId), "That tier has already been given to another painting.");
        }
        else if (string.IsNullOrWhiteSpace(vm.CustomAwardTitle))
        {
            ModelState.AddModelError(nameof(vm.CompetitionAwardId), "Pick an award tier or type a custom title.");
        }
    }

    private async Task<string> NextCertificateNumberAsync()
    {
        var year = DateTime.Now.Year;
        var n = await Db.Awards.CountAsync(a => a.CertificateNumber.StartsWith($"IFA-{year}-")) + 1;
        string number;
        do number = $"IFA-{year}-{n++:D4}";
        while (await Db.Awards.AnyAsync(a => a.CertificateNumber == number));
        return number;
    }
}

/// <summary>Printable certificate – visible to the winner and to institute staff.</summary>
[Authorize]
public class CertificatesController : PortalController
{
    public CertificatesController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Certificate(int id)
    {
        var a = await Db.Awards.AsNoTracking().Include(x => x.Student).Include(x => x.Competition)
            .Include(x => x.CompetitionAward).Include(x => x.WinningSubmission).Include(x => x.IssuedBy)
            .FirstOrDefaultAsync(x => x.AwardId == id);
        if (a is null) return NotFound();

        if (User.IsInRole(Roles.Student) && a.Student.ApplicationUserId != UserId) return Forbid();

        ViewData["IsPortal"] = false;           // bare, print-friendly page
        ViewData["Bare"] = true;
        return View(a);
    }
}
