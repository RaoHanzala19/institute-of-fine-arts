using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InstituteOfFineArts.Domain;

public class Exhibition
{
    public int ExhibitionId { get; set; }

    [Required, StringLength(300)]
    public string ExhibitionName { get; set; } = string.Empty;

    [StringLength(3000)]
    public string? Description { get; set; }

    [Required, StringLength(300)]
    public string Venue { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = "Pakistan";

    public DateTime ExhibitionStartDate { get; set; }
    public DateTime ExhibitionEndDate { get; set; }

    public ExhibitionStatus Status { get; set; } = ExhibitionStatus.Upcoming;

    /// <summary>Share of every sale kept by the institute; the rest goes to the student.</summary>
    public decimal CommissionPercent { get; set; } = 10m;

    [StringLength(1000)]
    public string? AdditionalNotes { get; set; }

    public string? CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ExhibitionListing> ExhibitionListings { get; set; } = new List<ExhibitionListing>();
}

public class ExhibitionListing
{
    public int ExhibitionListingId { get; set; }

    public int ExhibitionId { get; set; }
    public Exhibition Exhibition { get; set; } = null!;

    public int SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public string? ForwardedById { get; set; }
    public ApplicationUser? ForwardedBy { get; set; }

    /// <summary>Asking price set by the student (painter).</summary>
    public decimal StudentQuotedPrice { get; set; }

    public SaleStatus SaleStatus { get; set; } = SaleStatus.Available;

    public decimal? FinalSalePrice { get; set; }
    public DateTime? DateSold { get; set; }

    [StringLength(30)]
    public string? ReceiptNumber { get; set; }

    [StringLength(150)]
    public string? BuyerName { get; set; }

    [StringLength(20)]
    public string? BuyerPhone { get; set; }

    [StringLength(150)]
    public string? BuyerEmail { get; set; }

    [StringLength(300)]
    public string? BuyerAddress { get; set; }

    /// <summary>Institute commission on the sale (snapshot taken when the sale is saved).</summary>
    public decimal? CommissionAmount { get; set; }

    /// <summary>What the student is owed: final price minus commission.</summary>
    public decimal? NetPayableToStudent { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotApplicable;
    public decimal? AmountPaidToStudent { get; set; }
    public DateTime? PaymentDate { get; set; }

    [StringLength(500)]
    public string? PaymentRemarks { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime ListedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public decimal? BalanceDue => NetPayableToStudent.HasValue
        ? NetPayableToStudent.Value - (AmountPaidToStudent ?? 0m)
        : null;
}
