using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Infrastructure;
using InstituteOfFineArts.Services;
using InstituteOfFineArts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Controllers;

[Authorize(Roles = Roles.Internal)]
public class ReportsController : PortalController
{
    public ReportsController(AppDbContext db) : base(db) { }

    public IActionResult Index() => View();

    public async Task<IActionResult> Submissions(int? competitionId, DateTime? from, DateTime? to, SubmissionGrade? grade, bool export = false)
    {
        var q = Db.Submissions.AsNoTracking().Include(s => s.Student).Include(s => s.Competition).Include(s => s.Evaluation).AsQueryable();
        if (competitionId.HasValue) q = q.Where(s => s.CompetitionId == competitionId);
        if (from.HasValue) q = q.Where(s => s.SubmittedAt >= from);
        if (to.HasValue) q = q.Where(s => s.SubmittedAt < to.Value.Date.AddDays(1));
        if (grade.HasValue) q = q.Where(s => s.Grade == grade);
        var rows = await q.OrderByDescending(s => s.SubmittedAt).ToListAsync();

        if (export)
            return Csv.File("submission-report",
                new[] { "Painting", "Student", "Enrollment", "Competition", "Submitted", "Late", "Grade", "Evaluated on" },
                rows.Select(s => new object?[] { s.PaintingTitle, s.Student.FullName, s.Student.EnrollmentNumber, s.Competition.Title, s.SubmittedAt, s.IsLateSubmission ? "Yes" : "No", s.Grade, s.Evaluation?.EvaluatedAt }));

        var vm = new SubmissionReportVm
        {
            CompetitionId = competitionId, From = from, To = to, Grade = grade, Rows = rows,
            Competitions = await Db.Competitions.AsNoTracking().OrderByDescending(c => c.StartDate).ToListAsync()
        };
        foreach (var g in Enum.GetValues<SubmissionGrade>())
        {
            vm.GradeMix.Labels.Add(g.DisplayName());
            vm.GradeMix.Values.Add(rows.Count(r => r.Grade == g));
        }
        return View(vm);
    }

    public async Task<IActionResult> Sales(int? exhibitionId, bool export = false)
    {
        var q = Db.ExhibitionListings.AsNoTracking().Include(l => l.Exhibition)
            .Include(l => l.Submission).ThenInclude(s => s.Student)
            .Where(l => l.SaleStatus == SaleStatus.Sold);
        if (exhibitionId.HasValue) q = q.Where(l => l.ExhibitionId == exhibitionId);
        var rows = await q.OrderByDescending(l => l.DateSold).ToListAsync();

        if (export)
            return Csv.File("sales-report",
                new[] { "Receipt", "Painting", "Student", "Exhibition", "Sold on", "Buyer", "Sale price", "Commission", "Net to student", "Paid", "Balance due", "Payment" },
                rows.Select(l => new object?[] { l.ReceiptNumber, l.Submission.PaintingTitle, l.Submission.Student.FullName, l.Exhibition.ExhibitionName, l.DateSold, l.BuyerName, l.FinalSalePrice, l.CommissionAmount, l.NetPayableToStudent, l.AmountPaidToStudent, l.BalanceDue, l.PaymentStatus }));

        return View(new SalesReportVm
        {
            ExhibitionId = exhibitionId, Rows = rows,
            Exhibitions = await Db.Exhibitions.AsNoTracking().OrderByDescending(e => e.ExhibitionStartDate).ToListAsync(),
            Gross = rows.Sum(r => r.FinalSalePrice ?? 0),
            Commission = rows.Sum(r => r.CommissionAmount ?? 0),
            NetToStudents = rows.Sum(r => r.NetPayableToStudent ?? 0),
            Paid = rows.Sum(r => r.AmountPaidToStudent ?? 0),
            Outstanding = rows.Sum(r => r.BalanceDue ?? 0)
        });
    }

    public async Task<IActionResult> Competitions(bool export = false)
    {
        var comps = await Db.Competitions.AsNoTracking().OrderByDescending(c => c.StartDate)
            .Select(c => new CompetitionReportRow
            {
                Competition = c,
                Submissions = c.Submissions.Count,
                Evaluated = c.Submissions.Count(s => s.Grade != SubmissionGrade.Pending),
                Best = c.Submissions.Count(s => s.Grade == SubmissionGrade.Best),
                Disqualified = c.Submissions.Count(s => s.Grade == SubmissionGrade.Disqualified),
                Awards = c.IssuedAwards.Count,
                PrizeTotal = c.IssuedAwards.Sum(a => a.CompetitionAward!.PrizeMoney ?? 0)
            }).ToListAsync();

        if (export)
            return Csv.File("competition-summary",
                new[] { "Competition", "Status", "Opens", "Deadline", "Submissions", "Evaluated", "Best", "Disqualified", "Awards", "Prize money awarded" },
                comps.Select(r => new object?[] { r.Competition.Title, r.Competition.Status, r.Competition.StartDate, r.Competition.EndDate, r.Submissions, r.Evaluated, r.Best, r.Disqualified, r.Awards, r.PrizeTotal }));

        return View(comps);
    }
}
