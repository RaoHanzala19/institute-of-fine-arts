using System.ComponentModel.DataAnnotations;
using InstituteOfFineArts.Domain;

namespace InstituteOfFineArts.ViewModels;

// ── Account ─────────────────────────────────────────────────────────────────────
public class LoginVm
{
    [Required, EmailAddress, Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ProfileVm
{
    [Required, StringLength(100), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100), Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    [Phone, StringLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    public bool HasProfileRecord { get; set; }
}

public class ChangePasswordVm
{
    [Required, DataType(DataType.Password), Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8), Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "The two passwords do not match."), Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordVm
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8), Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "The two passwords do not match."), Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ContactVm
{
    [Required, StringLength(120), Display(Name = "Your name")]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150), Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(3000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;
}

// ── People ──────────────────────────────────────────────────────────────────────
public class StudentFormVm
{
    public int StudentId { get; set; }

    [Required, StringLength(100), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100), Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress, StringLength(150), Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }

    [Required, StringLength(20)]
    public string Gender { get; set; } = string.Empty;

    [Required, DataType(DataType.Date), Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, Phone, StringLength(20), Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(100), Display(Name = "Parent / guardian")]
    public string? ParentOrGuardianName { get; set; }

    [Phone, StringLength(20), Display(Name = "Guardian phone")]
    public string? ParentOrGuardianPhone { get; set; }

    [Required, StringLength(50), Display(Name = "Enrollment number")]
    public string EnrollmentNumber { get; set; } = string.Empty;

    [Required, DataType(DataType.Date), Display(Name = "Admission date")]
    public DateTime? AdmissionDate { get; set; } = DateTime.Today;

    [Required, StringLength(150)]
    public string Course { get; set; } = string.Empty;

    [StringLength(100), Display(Name = "Class / batch")]
    public string? ClassName { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;
}

public class StaffFormVm
{
    public int StaffId { get; set; }

    [Required, StringLength(100), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100), Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress, StringLength(150), Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }

    [Required, StringLength(20)]
    public string Gender { get; set; } = string.Empty;

    [Required, DataType(DataType.Date), Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, Phone, StringLength(20), Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(100), Display(Name = "Emergency contact")]
    public string? EmergencyContact { get; set; }

    [StringLength(200)]
    public string? Qualification { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Date joined")]
    public DateTime? DateJoined { get; set; } = DateTime.Today;

    [StringLength(300), Display(Name = "Classes handled")]
    public string? HandlesClass { get; set; }

    [StringLength(300), Display(Name = "Subjects handled")]
    public string? HandlesSubject { get; set; }

    [StringLength(100)]
    public string? Designation { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UserFormVm
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150), Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = Roles.Manager;
}

// ── Competitions ────────────────────────────────────────────────────────────────
public class AwardTierVm
{
    public int? CompetitionAwardId { get; set; }

    [StringLength(100), Display(Name = "Title")]
    public string? AwardTitle { get; set; }

    [StringLength(500), Display(Name = "Description")]
    public string? AwardDescription { get; set; }

    [Range(0, 100000000), Display(Name = "Prize money")]
    public decimal? PrizeMoney { get; set; }

    [Range(1, 50)]
    public int Rank { get; set; } = 1;
}

public class CompetitionFormVm : IValidatableObject
{
    public int CompetitionId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Theme { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(3000), Display(Name = "Rules & conditions")]
    public string? Conditions { get; set; }

    [Required, Display(Name = "Opens")]
    public DateTime? StartDate { get; set; } = DateTime.Today;

    [Required, Display(Name = "Deadline")]
    public DateTime? EndDate { get; set; } = DateTime.Today.AddDays(14).AddHours(23).AddMinutes(59);

    public CompetitionStatus Status { get; set; } = CompetitionStatus.Upcoming;

    public List<AwardTierVm> Awards { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate <= StartDate)
            yield return new ValidationResult("The deadline must be after the opening date.", new[] { nameof(EndDate) });

        var titles = Awards.Where(a => !string.IsNullOrWhiteSpace(a.AwardTitle)).Select(a => a.Rank).ToList();
        if (titles.Count != titles.Distinct().Count())
            yield return new ValidationResult("Each award tier needs its own rank.", new[] { nameof(Awards) });
    }
}

// ── Submissions ─────────────────────────────────────────────────────────────────
public class SubmissionFormVm
{
    public int SubmissionId { get; set; }

    [Required(ErrorMessage = "Please choose a competition."), Display(Name = "Competition")]
    public int CompetitionId { get; set; }

    [Required, StringLength(200), Display(Name = "Painting title")]
    public string PaintingTitle { get; set; } = string.Empty;

    [Required, Display(Name = "Text type")]
    public string TextType { get; set; } = "Description";

    [Required, StringLength(5000, MinimumLength = 10), Display(Name = "Poem / quotation / story / description")]
    public string AccompanyingText { get; set; } = string.Empty;

    [StringLength(3000), Display(Name = "Creative notes")]
    public string? CreativeDescription { get; set; }

    [Display(Name = "Painting (JPEG)")]
    public IFormFile? PaintingFile { get; set; }

    [Display(Name = "I understand the deadline has passed and this entry will be disqualified")]
    public bool AcceptLate { get; set; }

    public string? ExistingImagePath { get; set; }
}

public class EvaluateVm
{
    public int SubmissionId { get; set; }

    [Display(Name = "Grade")]
    public SubmissionGrade Grade { get; set; } = SubmissionGrade.Good;

    [StringLength(2000), Display(Name = "What works well")]
    public string? PositivePoints { get; set; }

    [StringLength(2000), Display(Name = "What holds it back")]
    public string? NegativePoints { get; set; }

    [StringLength(2000), Display(Name = "Areas of improvement")]
    public string? AreasOfImprovement { get; set; }

    [StringLength(2000), Display(Name = "General remarks")]
    public string? GeneralRemarks { get; set; }
}

// ── Awards ──────────────────────────────────────────────────────────────────────
public class AwardFormVm
{
    public int AwardId { get; set; }

    [Required(ErrorMessage = "Choose a competition."), Display(Name = "Competition")]
    public int CompetitionId { get; set; }

    [Required(ErrorMessage = "Choose the winning painting."), Display(Name = "Winning painting")]
    public int SubmissionId { get; set; }

    [Display(Name = "Award tier")]
    public int? CompetitionAwardId { get; set; }

    [StringLength(200), Display(Name = "Custom title (optional)")]
    public string? CustomAwardTitle { get; set; }

    [StringLength(2000)]
    public string? Remarks { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Date issued")]
    public DateTime? DateIssued { get; set; } = DateTime.Today;
}

// ── Exhibitions ─────────────────────────────────────────────────────────────────
public class ExhibitionFormVm : IValidatableObject
{
    public int ExhibitionId { get; set; }

    [Required, StringLength(300), Display(Name = "Exhibition name")]
    public string ExhibitionName { get; set; } = string.Empty;

    [StringLength(3000)]
    public string? Description { get; set; }

    [Required, StringLength(300)]
    public string Venue { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = "Pakistan";

    [Required, DataType(DataType.Date), Display(Name = "Starts")]
    public DateTime? ExhibitionStartDate { get; set; } = DateTime.Today.AddDays(30);

    [Required, DataType(DataType.Date), Display(Name = "Ends")]
    public DateTime? ExhibitionEndDate { get; set; } = DateTime.Today.AddDays(37);

    public ExhibitionStatus Status { get; set; } = ExhibitionStatus.Upcoming;

    [Range(0, 90), Display(Name = "Institute commission (%)")]
    public decimal CommissionPercent { get; set; } = 10m;

    [StringLength(1000), Display(Name = "Additional notes")]
    public string? AdditionalNotes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExhibitionStartDate.HasValue && ExhibitionEndDate.HasValue && ExhibitionEndDate < ExhibitionStartDate)
            yield return new ValidationResult("The end date cannot be before the start date.", new[] { nameof(ExhibitionEndDate) });
    }
}

public class ListingCreateVm
{
    [Required(ErrorMessage = "Choose an exhibition."), Display(Name = "Exhibition")]
    public int ExhibitionId { get; set; }

    [Required(ErrorMessage = "Choose a painting."), Display(Name = "Painting")]
    public int SubmissionId { get; set; }

    [Required, Range(1, 100000000), Display(Name = "Price quoted by the student")]
    public decimal? StudentQuotedPrice { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class SaleVm
{
    public int ExhibitionListingId { get; set; }

    public SaleStatus SaleStatus { get; set; } = SaleStatus.Available;

    [Range(0, 100000000), Display(Name = "Final sale price")]
    public decimal? FinalSalePrice { get; set; }

    [DataType(DataType.Date), Display(Name = "Date sold")]
    public DateTime? DateSold { get; set; }

    [StringLength(150), Display(Name = "Buyer name")]
    public string? BuyerName { get; set; }

    [Phone, StringLength(20), Display(Name = "Buyer phone")]
    public string? BuyerPhone { get; set; }

    [EmailAddress, StringLength(150), Display(Name = "Buyer email")]
    public string? BuyerEmail { get; set; }

    [StringLength(300), Display(Name = "Buyer address")]
    public string? BuyerAddress { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotApplicable;

    [Range(0, 100000000), Display(Name = "Amount paid to student")]
    public decimal? AmountPaidToStudent { get; set; }

    [DataType(DataType.Date), Display(Name = "Payment date")]
    public DateTime? PaymentDate { get; set; }

    [StringLength(500), Display(Name = "Payment remarks")]
    public string? PaymentRemarks { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

// ── Announcements ───────────────────────────────────────────────────────────────
public class AnnouncementFormVm
{
    public int AnnouncementId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.Everyone;

    [Display(Name = "Pin to the top")]
    public bool IsPinned { get; set; }

    [DataType(DataType.Date), Display(Name = "Expires on (optional)")]
    public DateTime? ExpiresAt { get; set; }
}
