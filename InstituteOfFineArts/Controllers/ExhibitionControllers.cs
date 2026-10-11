using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Infrastructure;
using InstituteOfFineArts.Services;
using InstituteOfFineArts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Controllers;

public class ExhibitionRow
{
    public Exhibition Exhibition { get; set; } = null!;
    public int Listings { get; set; }
    public int Sold { get; set; }
    public decimal Revenue { get; set; }
}

public class ExhibitionDetailsVm
{
    public Exhibition Exhibition { get; set; } = null!;
    public List<ExhibitionListing> Listings { get; set; } = new();
    public decimal Gross { get; set; }
    public decimal Commission { get; set; }
    public decimal Outstanding { get; set; }
}

// ══ Exhibitions ═══════════════════════════════════════════════════════════════
[Authorize(Roles = Roles.Exhibitors)]
public class ExhibitionsController : PortalController
{
    public ExhibitionsController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, ExhibitionStatus? status, int page = 1, bool export = false)
    {
        var query = Db.Exhibitions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(e => e.ExhibitionName.Contains(q) || e.City.Contains(q) || e.Venue.Contains(q));
        if (status.HasValue) query = query.Where(e => e.Status == status);

        var rows = query.OrderByDescending(e => e.ExhibitionStartDate).Select(e => new ExhibitionRow
        {
            Exhibition = e,
            Listings = e.ExhibitionListings.Count,
            Sold = e.ExhibitionListings.Count(l => l.SaleStatus == SaleStatus.Sold),
            Revenue = e.ExhibitionListings.Where(l => l.SaleStatus == SaleStatus.Sold).Sum(l => l.FinalSalePrice ?? 0)
        });

        if (export)
        {
            var all = await rows.ToListAsync();
            return Csv.File("exhibitions",
                new[] { "Name", "Venue", "City", "Starts", "Ends", "Status", "Listings", "Sold", "Revenue" },
                all.Select(r => new object?[] { r.Exhibition.ExhibitionName, r.Exhibition.Venue, r.Exhibition.City, r.Exhibition.ExhibitionStartDate, r.Exhibition.ExhibitionEndDate, r.Exhibition.Status, r.Listings, r.Sold, r.Revenue }));
        }

        ViewBag.Q = q; ViewBag.Status = status;
        return View(await PagedList<ExhibitionRow>.CreateAsync(rows, page, 9));
    }

    public async Task<IActionResult> Details(int id)
    {
        var e = await Db.Exhibitions.AsNoTracking().Include(x => x.CreatedBy).FirstOrDefaultAsync(x => x.ExhibitionId == id);
        if (e is null) return NotFound();

        var listings = await Db.ExhibitionListings.AsNoTracking()
            .Include(l => l.Submission).ThenInclude(s => s.Student)
            .Where(l => l.ExhibitionId == id).OrderBy(l => l.ListedAt).ToListAsync();

        var sold = listings.Where(l => l.SaleStatus == SaleStatus.Sold).ToList();
        return View(new ExhibitionDetailsVm
        {
            Exhibition = e, Listings = listings,
            Gross = sold.Sum(l => l.FinalSalePrice ?? 0),
            Commission = sold.Sum(l => l.CommissionAmount ?? 0),
            Outstanding = sold.Sum(l => l.BalanceDue ?? 0)
        });
    }

    public IActionResult Create() => View("Form", new ExhibitionFormVm { CommissionPercent = 10m });

    [HttpPost]
    public async Task<IActionResult> Create(ExhibitionFormVm vm)
    {
        if (!ModelState.IsValid) return View("Form", vm);

        var e = new Exhibition { CreatedById = UserId };
        Apply(e, vm, isNew: true);
        Db.Exhibitions.Add(e);
        await Db.SaveChangesAsync();

        Success($"Exhibition “{e.ExhibitionName}” created. Forward the best paintings to it next.");
        return RedirectToAction(nameof(Details), new { id = e.ExhibitionId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var e = await Db.Exhibitions.AsNoTracking().FirstOrDefaultAsync(x => x.ExhibitionId == id);
        if (e is null) return NotFound();
        return View("Form", new ExhibitionFormVm
        {
            ExhibitionId = e.ExhibitionId, ExhibitionName = e.ExhibitionName, Description = e.Description, Venue = e.Venue,
            City = e.City, Country = e.Country, ExhibitionStartDate = e.ExhibitionStartDate, ExhibitionEndDate = e.ExhibitionEndDate,
            Status = e.Status, CommissionPercent = e.CommissionPercent, AdditionalNotes = e.AdditionalNotes
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ExhibitionFormVm vm)
    {
        if (id != vm.ExhibitionId) return BadRequest();
        if (!ModelState.IsValid) return View("Form", vm);

        var e = await Db.Exhibitions.FirstOrDefaultAsync(x => x.ExhibitionId == id);
        if (e is null) return NotFound();

        Apply(e, vm, isNew: false);
        await Db.SaveChangesAsync();
        Success("Exhibition updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await Db.Exhibitions.Include(x => x.ExhibitionListings).FirstOrDefaultAsync(x => x.ExhibitionId == id);
        if (e is null) return NotFound();
        if (e.ExhibitionListings.Count > 0)
        {
            Error("This exhibition has paintings listed. Remove them first, or mark the exhibition as cancelled.");
            return RedirectToAction(nameof(Details), new { id });
        }
        Db.Exhibitions.Remove(e);
        await Db.SaveChangesAsync();
        Success("Exhibition deleted.");
        return RedirectToAction(nameof(Index));
    }

    private static void Apply(Exhibition e, ExhibitionFormVm vm, bool isNew)
    {
        e.ExhibitionName = vm.ExhibitionName.Trim();
        e.Description = vm.Description;
        e.Venue = vm.Venue.Trim();
        e.City = vm.City.Trim();
        e.Country = vm.Country.Trim();
        e.ExhibitionStartDate = vm.ExhibitionStartDate!.Value;
        e.ExhibitionEndDate = vm.ExhibitionEndDate!.Value;
        e.CommissionPercent = vm.CommissionPercent;
        e.AdditionalNotes = vm.AdditionalNotes;

        if (isNew && vm.Status != ExhibitionStatus.Cancelled)
        {
            var today = DateTime.Today;
            e.Status = today > e.ExhibitionEndDate ? ExhibitionStatus.Concluded
                : today >= e.ExhibitionStartDate ? ExhibitionStatus.Ongoing : ExhibitionStatus.Upcoming;
        }
        else
        {
            e.Status = vm.Status;
            if (!isNew) e.UpdatedAt = DateTime.Now;
        }
    }
}

// ══ Listings & sales ══════════════════════════════════════════════════════════
[Authorize(Roles = Roles.Exhibitors)]
public class ListingsController : PortalController
{
    private readonly NotificationService _notify;

    public ListingsController(AppDbContext db, NotificationService notify) : base(db) => _notify = notify;

    public async Task<IActionResult> Index(int? exhibitionId, SaleStatus? sale, PaymentStatus? payment, string? q,
        int page = 1, bool export = false)
    {
        var query = Db.ExhibitionListings.AsNoTracking().Include(l => l.Exhibition)
            .Include(l => l.Submission).ThenInclude(s => s.Student).AsQueryable();
        if (exhibitionId.HasValue) query = query.Where(l => l.ExhibitionId == exhibitionId);
        if (sale.HasValue) query = query.Where(l => l.SaleStatus == sale);
        if (payment.HasValue) query = query.Where(l => l.PaymentStatus == payment);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(l => l.Submission.PaintingTitle.Contains(q) || l.Submission.Student.FirstName.Contains(q) ||
                                     l.Submission.Student.LastName.Contains(q) || (l.BuyerName != null && l.BuyerName.Contains(q)));
        query = query.OrderByDescending(l => l.ListedAt);

        if (export)
        {
            var all = await query.ToListAsync();
            return Csv.File("exhibition-listings",
                new[] { "Painting", "Student", "Exhibition", "Asking price", "Status", "Final price", "Commission", "Net to student", "Paid", "Payment status", "Buyer" },
                all.Select(l => new object?[] { l.Submission.PaintingTitle, l.Submission.Student.FullName, l.Exhibition.ExhibitionName, l.StudentQuotedPrice, l.SaleStatus, l.FinalSalePrice, l.CommissionAmount, l.NetPayableToStudent, l.AmountPaidToStudent, l.PaymentStatus, l.BuyerName }));
        }

        ViewBag.Exhibitions = new SelectList(await Db.Exhibitions.OrderByDescending(e => e.ExhibitionStartDate).ToListAsync(),
            "ExhibitionId", "ExhibitionName", exhibitionId);
        ViewBag.ExhibitionId = exhibitionId; ViewBag.Sale = sale; ViewBag.Payment = payment; ViewBag.Q = q;
        return View(await PagedList<ExhibitionListing>.CreateAsync(query, page, 12));
    }

    public async Task<IActionResult> Create(int? exhibitionId, int? submissionId)
    {
        await FillCreateListsAsync();
        return View(new ListingCreateVm { ExhibitionId = exhibitionId ?? 0, SubmissionId = submissionId ?? 0 });
    }

    [HttpPost]
    public async Task<IActionResult> Create(ListingCreateVm vm)
    {
        var sub = await Db.Submissions.Include(s => s.Student).FirstOrDefaultAsync(s => s.SubmissionId == vm.SubmissionId);
        var ex = await Db.Exhibitions.FirstOrDefaultAsync(e => e.ExhibitionId == vm.ExhibitionId);

        if (sub is null) ModelState.AddModelError(nameof(vm.SubmissionId), "Painting not found.");
        else if (sub.Grade != SubmissionGrade.Best)
            ModelState.AddModelError(nameof(vm.SubmissionId), "Only paintings graded Best can be forwarded to an exhibition.");
        if (ex is null) ModelState.AddModelError(nameof(vm.ExhibitionId), "Exhibition not found.");
        else if (ex.Status is ExhibitionStatus.Concluded or ExhibitionStatus.Cancelled)
            ModelState.AddModelError(nameof(vm.ExhibitionId), "This exhibition is no longer accepting paintings.");
        if (sub != null && ex != null &&
            await Db.ExhibitionListings.AnyAsync(l => l.ExhibitionId == ex.ExhibitionId && l.SubmissionId == sub.SubmissionId))
            ModelState.AddModelError(nameof(vm.SubmissionId), "This painting is already listed in that exhibition.");

        if (!ModelState.IsValid)
        {
            await FillCreateListsAsync();
            return View(vm);
        }

        var listing = new ExhibitionListing
        {
            ExhibitionId = ex!.ExhibitionId, SubmissionId = sub!.SubmissionId, ForwardedById = UserId,
            StudentQuotedPrice = vm.StudentQuotedPrice!.Value, Notes = vm.Notes
        };
        Db.ExhibitionListings.Add(listing);
        _notify.Notify(sub!.Student.ApplicationUserId, $"“{sub.PaintingTitle}” is going to an exhibition",
            $"It has been selected for {ex.ExhibitionName} in {ex.City}. Your asking price: {Ui.Money(listing.StudentQuotedPrice)}.",
            Url.Action("Exhibitions", "Student"), NotificationType.Success);
        await Db.SaveChangesAsync();

        Success("Painting forwarded to the exhibition and the student has been told.");
        return RedirectToAction("Details", "Exhibitions", new { id = ex.ExhibitionId });
    }

    public async Task<IActionResult> Sale(int id)
    {
        var l = await LoadAsync(id);
        if (l is null) return NotFound();
        ViewBag.Listing = l;
        return View(new SaleVm
        {
            ExhibitionListingId = l.ExhibitionListingId, SaleStatus = l.SaleStatus, FinalSalePrice = l.FinalSalePrice ?? l.StudentQuotedPrice,
            DateSold = l.DateSold ?? DateTime.Today, BuyerName = l.BuyerName, BuyerPhone = l.BuyerPhone, BuyerEmail = l.BuyerEmail,
            BuyerAddress = l.BuyerAddress, PaymentStatus = l.PaymentStatus, AmountPaidToStudent = l.AmountPaidToStudent,
            PaymentDate = l.PaymentDate, PaymentRemarks = l.PaymentRemarks, Notes = l.Notes
        });
    }

    [HttpPost]
    public async Task<IActionResult> Sale(int id, SaleVm vm)
    {
        var l = await LoadAsync(id);
        if (l is null || id != vm.ExhibitionListingId) return NotFound();

        var wasSold = l.SaleStatus == SaleStatus.Sold;
        var wasPaid = l.PaymentStatus == PaymentStatus.Paid;
        decimal commission = 0, net = 0;

        if (vm.SaleStatus == SaleStatus.Sold)
        {
            if (!(vm.FinalSalePrice > 0)) ModelState.AddModelError(nameof(vm.FinalSalePrice), "Enter the price the painting sold for.");
            if (vm.DateSold is null) ModelState.AddModelError(nameof(vm.DateSold), "Enter the date of sale.");
            if (string.IsNullOrWhiteSpace(vm.BuyerName)) ModelState.AddModelError(nameof(vm.BuyerName), "Enter the buyer's name.");

            if (vm.FinalSalePrice > 0)
            {
                commission = Math.Round(vm.FinalSalePrice.Value * l.Exhibition.CommissionPercent / 100m, 2);
                net = vm.FinalSalePrice.Value - commission;
            }

            if (vm.PaymentStatus == PaymentStatus.Paid)
            {
                if (vm.PaymentDate is null) ModelState.AddModelError(nameof(vm.PaymentDate), "Enter the payment date.");
                if (!(vm.AmountPaidToStudent > 0)) ModelState.AddModelError(nameof(vm.AmountPaidToStudent), "Enter the amount paid to the student.");
                else if (vm.FinalSalePrice > 0 && vm.AmountPaidToStudent > net)
                    ModelState.AddModelError(nameof(vm.AmountPaidToStudent), $"The student is owed at most {Ui.Money(net, true)} after commission.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Listing = l;
            return View(vm);
        }

        l.SaleStatus = vm.SaleStatus;
        l.Notes = vm.Notes;
        l.UpdatedAt = DateTime.Now;

        if (vm.SaleStatus == SaleStatus.Sold)
        {
            l.FinalSalePrice = vm.FinalSalePrice;
            l.DateSold = vm.DateSold;
            l.BuyerName = vm.BuyerName?.Trim(); l.BuyerPhone = vm.BuyerPhone; l.BuyerEmail = vm.BuyerEmail; l.BuyerAddress = vm.BuyerAddress;
            l.CommissionAmount = commission;
            l.NetPayableToStudent = net;
            l.ReceiptNumber ??= $"RCP-{DateTime.Now.Year}-{l.ExhibitionListingId:D5}";
            l.PaymentStatus = vm.PaymentStatus == PaymentStatus.NotApplicable ? PaymentStatus.Pending : vm.PaymentStatus;
            l.AmountPaidToStudent = l.PaymentStatus == PaymentStatus.Paid ? vm.AmountPaidToStudent : null;
            l.PaymentDate = l.PaymentStatus == PaymentStatus.Paid ? vm.PaymentDate : null;
            l.PaymentRemarks = vm.PaymentRemarks;
        }
        else
        {
            l.FinalSalePrice = null; l.DateSold = null; l.BuyerName = l.BuyerPhone = l.BuyerEmail = l.BuyerAddress = null;
            l.CommissionAmount = null; l.NetPayableToStudent = null;
            l.PaymentStatus = PaymentStatus.NotApplicable; l.AmountPaidToStudent = null; l.PaymentDate = null; l.PaymentRemarks = null;
        }

        var studentUser = l.Submission.Student.ApplicationUserId;
        if (l.SaleStatus == SaleStatus.Sold && !wasSold)
            _notify.Notify(studentUser, $"🎉 “{l.Submission.PaintingTitle}” has sold!",
                $"Sold for {Ui.Money(l.FinalSalePrice)}. After the institute's {l.Exhibition.CommissionPercent:0.#}% commission you will receive {Ui.Money(l.NetPayableToStudent)}.",
                Url.Action("Exhibitions", "Student"), NotificationType.Success);
        if (l.PaymentStatus == PaymentStatus.Paid && !wasPaid)
            _notify.Notify(studentUser, "Payment sent to you",
                $"{Ui.Money(l.AmountPaidToStudent)} for “{l.Submission.PaintingTitle}” was paid on {Ui.Date(l.PaymentDate)}.",
                Url.Action("Exhibitions", "Student"), NotificationType.Success);

        await Db.SaveChangesAsync();
        Success("Sale details saved.");
        return RedirectToAction("Details", "Exhibitions", new { id = l.ExhibitionId });
    }

    public async Task<IActionResult> Receipt(int id)
    {
        var l = await LoadAsync(id, tracking: false);
        if (l is null) return NotFound();
        if (l.SaleStatus != SaleStatus.Sold)
        {
            Error("Only sold paintings have a receipt.");
            return RedirectToAction(nameof(Sale), new { id });
        }
        ViewData["IsPortal"] = false;
        ViewData["Bare"] = true;
        return View(l);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var l = await Db.ExhibitionListings.Include(x => x.Submission).FirstOrDefaultAsync(x => x.ExhibitionListingId == id);
        if (l is null) return NotFound();
        if (l.SaleStatus == SaleStatus.Sold)
        {
            Error("A sold painting cannot be removed from the records. Mark it as returned first if the sale fell through.");
            return RedirectToAction(nameof(Sale), new { id });
        }
        var exhibitionId = l.ExhibitionId;
        Db.ExhibitionListings.Remove(l);
        await Db.SaveChangesAsync();
        Success("Painting removed from the exhibition.");
        return RedirectToAction("Details", "Exhibitions", new { id = exhibitionId });
    }

    private Task<ExhibitionListing?> LoadAsync(int id, bool tracking = true)
    {
        var q = Db.ExhibitionListings.Include(l => l.Exhibition).Include(l => l.ForwardedBy)
            .Include(l => l.Submission).ThenInclude(s => s.Student).AsQueryable();
        if (!tracking) q = q.AsNoTracking();
        return q.FirstOrDefaultAsync(l => l.ExhibitionListingId == id);
    }

    private async Task FillCreateListsAsync()
    {
        ViewBag.Exhibitions = await Db.Exhibitions.AsNoTracking()
            .Where(e => e.Status == ExhibitionStatus.Upcoming || e.Status == ExhibitionStatus.Ongoing)
            .OrderBy(e => e.ExhibitionStartDate).ToListAsync();
        ViewBag.Paintings = await Db.Submissions.AsNoTracking().Include(s => s.Student).Include(s => s.Competition)
            .Where(s => s.Grade == SubmissionGrade.Best).OrderBy(s => s.PaintingTitle).ToListAsync();
    }
}
