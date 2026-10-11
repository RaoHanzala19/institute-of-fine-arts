using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InstituteOfFineArts.Domain;

public class Competition
{
    public int CompetitionId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Theme { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(3000)]
    public string? Conditions { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public CompetitionStatus Status { get; set; } = CompetitionStatus.Upcoming;

    public string? CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CompetitionAward> CompetitionAwards { get; set; } = new List<CompetitionAward>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<Award> IssuedAwards { get; set; } = new List<Award>();

    /// <summary>True once the deadline has passed (regardless of the stored status).</summary>
    [NotMapped]
    public bool DeadlinePassed => DateTime.Now > EndDate;

    /// <summary>Whole days left until the deadline (0 when passed).</summary>
    [NotMapped]
    public int DaysLeft => DeadlinePassed ? 0 : (int)Math.Ceiling((EndDate - DateTime.Now).TotalDays);

    /// <summary>Students can still upload while the competition is not concluded and has started.</summary>
    [NotMapped]
    public bool AcceptsSubmissions => Status != CompetitionStatus.Concluded && DateTime.Now >= StartDate;
}

public class CompetitionAward
{
    public int CompetitionAwardId { get; set; }

    public int CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    [Required, StringLength(100)]
    public string AwardTitle { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AwardDescription { get; set; }

    public decimal? PrizeMoney { get; set; }

    public int Rank { get; set; }

    public ICollection<Award> IssuedAwards { get; set; } = new List<Award>();
}

public class Submission
{
    public int SubmissionId { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    [Required, StringLength(200)]
    public string PaintingTitle { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string ImageFilePath { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ThumbnailPath { get; set; }

    [StringLength(255)]
    public string? OriginalFileName { get; set; }

    public long FileSizeInBytes { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    /// <summary>Poem / Quotation / Story / Description submitted with the painting.</summary>
    [Required, StringLength(5000)]
    public string AccompanyingText { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string TextType { get; set; } = "Description";

    [StringLength(3000)]
    public string? CreativeDescription { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.Now;
    public DateTime? LastUpdatedAt { get; set; }

    /// <summary>Submitted after the competition deadline: automatically disqualified.</summary>
    public bool IsLateSubmission { get; set; }

    public SubmissionGrade Grade { get; set; } = SubmissionGrade.Pending;

    public Evaluation? Evaluation { get; set; }
    public ICollection<ExhibitionListing> ExhibitionListings { get; set; } = new List<ExhibitionListing>();
    public ICollection<PaintingLike> Likes { get; set; } = new List<PaintingLike>();
}

public class Evaluation
{
    public int EvaluationId { get; set; }

    public int SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public string? EvaluatedById { get; set; }
    public ApplicationUser? EvaluatedBy { get; set; }

    public SubmissionGrade Grade { get; set; }

    [StringLength(2000)]
    public string? PositivePoints { get; set; }

    [StringLength(2000)]
    public string? NegativePoints { get; set; }

    [StringLength(2000)]
    public string? AreasOfImprovement { get; set; }

    [StringLength(2000)]
    public string? GeneralRemarks { get; set; }

    public DateTime EvaluatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

public class Award
{
    public int AwardId { get; set; }

    /// <summary>Human friendly number printed on the certificate, e.g. IFA-2026-0007.</summary>
    [Required, StringLength(30)]
    public string CertificateNumber { get; set; } = string.Empty;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public int? CompetitionAwardId { get; set; }
    public CompetitionAward? CompetitionAward { get; set; }

    public int SubmissionId { get; set; }
    public Submission WinningSubmission { get; set; } = null!;

    public string? IssuedById { get; set; }
    public ApplicationUser? IssuedBy { get; set; }

    [StringLength(200)]
    public string? CustomAwardTitle { get; set; }

    [StringLength(2000)]
    public string? Remarks { get; set; }

    public DateTime DateIssued { get; set; } = DateTime.Today;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public string DisplayTitle => !string.IsNullOrWhiteSpace(CustomAwardTitle)
        ? CustomAwardTitle!
        : CompetitionAward?.AwardTitle ?? "Award of Excellence";
}
