using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Infrastructure;
using InstituteOfFineArts.Services;
using InstituteOfFineArts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Controllers;

public class CompetitionRow
{
    public Competition Competition { get; set; } = null!;
    public int Submissions { get; set; }
    public int Tiers { get; set; }
    public decimal PrizePool { get; set; }
}

public class CompetitionDetailsVm
{
    public Competition Competition { get; set; } = null!;
    public List<CompetitionAward> Tiers { get; set; } = new();
    public List<Submission> Submissions { get; set; } = new();
    public List<Award> Awards { get; set; } = new();
    public ChartData GradeMix { get; set; } = new();
}

[Authorize(Roles = Roles.Internal)]
public class CompetitionsController : PortalController
{
    private readonly NotificationService _notify;

    public CompetitionsController(AppDbContext db, NotificationService notify) : base(db) => _notify = notify;

    public async Task<IActionResult> Index(string? q, CompetitionStatus? status, int page = 1, bool export = false)
    {
        var query = Db.Competitions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c => c.Title.Contains(q) || (c.Theme != null && c.Theme.Contains(q)));
        if (status.HasValue) query = query.Where(c => c.Status == status);

        var rows = query.OrderByDescending(c => c.StartDate).Select(c => new CompetitionRow
        {
            Competition = c,
            Submissions = c.Submissions.Count,
            Tiers = c.CompetitionAwards.Count,
            PrizePool = c.CompetitionAwards.Sum(t => t.PrizeMoney ?? 0)
        });

        if (export)
        {
            var all = await rows.ToListAsync();
            return Csv.File("competitions",
                new[] { "Title", "Theme", "Opens", "Deadline", "Status", "Submissions", "Award tiers", "Prize pool" },
                all.Select(r => new object?[] { r.Competition.Title, r.Competition.Theme, r.Competition.StartDate, r.Competition.EndDate, r.Competition.Status, r.Submissions, r.Tiers, r.PrizePool }));
        }

        ViewBag.Q = q;
        ViewBag.Status = status;
        return View(await PagedList<CompetitionRow>.CreateAsync(rows, page, 9));
    }

    public async Task<IActionResult> Details(int id)
    {
        var c = await Db.Competitions.AsNoTracking().Include(x => x.CreatedBy)
            .Include(x => x.CompetitionAwards).FirstOrDefaultAsync(x => x.CompetitionId == id);
        if (c is null) return NotFound();

        var subs = await Db.Submissions.AsNoTracking().Include(s => s.Student).Include(s => s.Evaluation)
            .Where(s => s.CompetitionId == id).OrderByDescending(s => s.SubmittedAt).ToListAsync();

        var vm = new CompetitionDetailsVm
        {
            Competition = c,
            Tiers = c.CompetitionAwards.OrderBy(t => t.Rank).ToList(),
            Submissions = subs,
            Awards = await Db.Awards.AsNoTracking().Include(a => a.Student).Include(a => a.CompetitionAward)
                .Where(a => a.CompetitionId == id).OrderBy(a => a.CompetitionAward!.Rank).ToListAsync()
        };
        foreach (var g in Enum.GetValues<SubmissionGrade>())
        {
            vm.GradeMix.Labels.Add(g.DisplayName());
            vm.GradeMix.Values.Add(subs.Count(s => s.Grade == g));
        }
        return View(vm);
    }

    // ── Create ────────────────────────────────────────────────────────────────
    [Authorize(Roles = Roles.Academic)]
    public IActionResult Create()
    {
        var vm = new CompetitionFormVm
        {
            Awards =
            {
                new AwardTierVm { Rank = 1, AwardTitle = "First Prize" },
                new AwardTierVm { Rank = 2, AwardTitle = "Second Prize" },
                new AwardTierVm { Rank = 3, AwardTitle = "Third Prize" }
            }
        };
        return View("Form", vm);
    }

    [HttpPost, Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Create(CompetitionFormVm vm)
    {
        if (!ModelState.IsValid) return View("Form", vm);

        var c = new Competition
        {
            Title = vm.Title.Trim(), Theme = vm.Theme, Description = vm.Description, Conditions = vm.Conditions,
            StartDate = vm.StartDate!.Value, EndDate = vm.EndDate!.Value,
            Status = vm.StartDate <= DateTime.Now ? CompetitionStatus.Ongoing : CompetitionStatus.Upcoming,
            CreatedById = UserId
        };
        foreach (var t in vm.Awards.Where(a => !string.IsNullOrWhiteSpace(a.AwardTitle)))
            c.CompetitionAwards.Add(new CompetitionAward
            {
                AwardTitle = t.AwardTitle!.Trim(), AwardDescription = t.AwardDescription, PrizeMoney = t.PrizeMoney, Rank = t.Rank
            });

        Db.Competitions.Add(c);
        await Db.SaveChangesAsync();

        await _notify.NotifyRoleAsync(Roles.Student, $"New competition: {c.Title}",
            $"Opens {Ui.Date(c.StartDate)} · deadline {Ui.Date(c.EndDate)}",
            Url.Action("Details", "Student", new { id = c.CompetitionId }), NotificationType.Info);
        await Db.SaveChangesAsync();

        Success($"Competition “{c.Title}” created and students have been notified.");
        return RedirectToAction(nameof(Details), new { id = c.CompetitionId });
    }

    // ── Edit ──────────────────────────────────────────────────────────────────
    [Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Edit(int id)
    {
        var c = await Db.Competitions.AsNoTracking().Include(x => x.CompetitionAwards)
            .FirstOrDefaultAsync(x => x.CompetitionId == id);
        if (c is null) return NotFound();

        return View("Form", new CompetitionFormVm
        {
            CompetitionId = c.CompetitionId, Title = c.Title, Theme = c.Theme, Description = c.Description,
            Conditions = c.Conditions, StartDate = c.StartDate, EndDate = c.EndDate, Status = c.Status,
            Awards = c.CompetitionAwards.OrderBy(t => t.Rank).Select(t => new AwardTierVm
            {
                CompetitionAwardId = t.CompetitionAwardId, AwardTitle = t.AwardTitle,
                AwardDescription = t.AwardDescription, PrizeMoney = t.PrizeMoney, Rank = t.Rank
            }).ToList()
        });
    }

    [HttpPost, Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Edit(int id, CompetitionFormVm vm)
    {
        if (id != vm.CompetitionId) return BadRequest();
        if (!ModelState.IsValid) return View("Form", vm);

        var c = await Db.Competitions.Include(x => x.CompetitionAwards).FirstOrDefaultAsync(x => x.CompetitionId == id);
        if (c is null) return NotFound();

        var incoming = vm.Awards.Where(a => !string.IsNullOrWhiteSpace(a.AwardTitle)).ToList();

        foreach (var tier in c.CompetitionAwards.ToList())
        {
            if (incoming.Any(i => i.CompetitionAwardId == tier.CompetitionAwardId)) continue;
            if (await Db.Awards.AnyAsync(a => a.CompetitionAwardId == tier.CompetitionAwardId))
            {
                ModelState.AddModelError("", $"“{tier.AwardTitle}” has already been issued to a student, so it cannot be removed.");
                return View("Form", vm);
            }
            Db.CompetitionAwards.Remove(tier);
        }

        foreach (var i in incoming)
        {
            var tier = c.CompetitionAwards.FirstOrDefault(t => t.CompetitionAwardId == i.CompetitionAwardId);
            if (tier is null)
            {
                c.CompetitionAwards.Add(new CompetitionAward
                {
                    AwardTitle = i.AwardTitle!.Trim(), AwardDescription = i.AwardDescription, PrizeMoney = i.PrizeMoney, Rank = i.Rank
                });
            }
            else
            {
                tier.AwardTitle = i.AwardTitle!.Trim();
                tier.AwardDescription = i.AwardDescription;
                tier.PrizeMoney = i.PrizeMoney;
                tier.Rank = i.Rank;
            }
        }

        var wasConcluded = c.Status == CompetitionStatus.Concluded;
        c.Title = vm.Title.Trim();
        c.Theme = vm.Theme;
        c.Description = vm.Description;
        c.Conditions = vm.Conditions;
        c.StartDate = vm.StartDate!.Value;
        c.EndDate = vm.EndDate!.Value;
        c.Status = vm.Status;
        c.UpdatedAt = DateTime.Now;

        await Db.SaveChangesAsync();

        if (!wasConcluded && c.Status == CompetitionStatus.Concluded) await NotifyConcludedAsync(c);

        Success("Competition updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Conclude(int id)
    {
        var c = await Db.Competitions.FirstOrDefaultAsync(x => x.CompetitionId == id);
        if (c is null) return NotFound();
        if (c.Status == CompetitionStatus.Concluded)
        {
            Warn("This competition is already concluded.");
            return RedirectToAction(nameof(Details), new { id });
        }

        c.Status = CompetitionStatus.Concluded;
        c.UpdatedAt = DateTime.Now;
        await Db.SaveChangesAsync();
        await NotifyConcludedAsync(c);

        Success($"“{c.Title}” is concluded. You can now issue awards to the winners.");
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task NotifyConcludedAsync(Competition c)
    {
        var students = await Db.Submissions.Where(s => s.CompetitionId == c.CompetitionId)
            .Select(s => s.Student.ApplicationUserId).Distinct().ToListAsync();
        foreach (var uid in students)
            _notify.Notify(uid, $"“{c.Title}” has concluded",
                "Judging is complete or under way — check your grades and feedback.",
                Url.Action("Submissions", "Student"), NotificationType.Info);
        await Db.SaveChangesAsync();
    }

    [HttpPost, Authorize(Roles = Roles.Academic)]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await Db.Competitions.Include(x => x.Submissions).FirstOrDefaultAsync(x => x.CompetitionId == id);
        if (c is null) return NotFound();

        if (c.Submissions.Count > 0)
        {
            Error("This competition already has submissions, so it cannot be deleted. Conclude it instead.");
            return RedirectToAction(nameof(Details), new { id });
        }

        Db.Competitions.Remove(c);
        await Db.SaveChangesAsync();
        Success("Competition deleted.");
        return RedirectToAction(nameof(Index));
    }
}
