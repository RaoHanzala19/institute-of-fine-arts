using System.ComponentModel.DataAnnotations;

namespace InstituteOfFineArts.Domain;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string Student = "Student";

    /// <summary>Roles that work inside the institute (everyone except students).</summary>
    public const string Internal = "Admin,Manager,Staff";

    /// <summary>Roles allowed to create / edit competitions, evaluate work and issue awards.</summary>
    public const string Academic = "Admin,Staff";

    /// <summary>Roles allowed to manage exhibitions, listings and sales.</summary>
    public const string Exhibitors = "Admin,Manager,Staff";

    public static readonly string[] All = { Admin, Manager, Staff, Student };
}

public enum CompetitionStatus
{
    Upcoming = 1,
    Ongoing = 2,
    Concluded = 3
}

public enum ExhibitionStatus
{
    Upcoming = 1,
    Ongoing = 2,
    Concluded = 3,
    Cancelled = 4
}

public enum SaleStatus
{
    Available = 1,
    Sold = 2,
    Returned = 3,
    Withdrawn = 4
}

public enum PaymentStatus
{
    [Display(Name = "Not applicable")] NotApplicable = 0,
    Pending = 1,
    Paid = 2
}

public enum SubmissionGrade
{
    Pending = 0,
    Best = 1,
    Better = 2,
    Good = 3,
    Moderate = 4,
    Normal = 5,
    Disqualified = 6
}

public enum NotificationType
{
    Info = 0,
    Success = 1,
    Warning = 2
}

public enum AnnouncementAudience
{
    Everyone = 0,
    Students = 1,
    Staff = 2
}

public static class TextTypes
{
    public static readonly string[] All = { "Description", "Poem", "Quotation", "Story" };
}
