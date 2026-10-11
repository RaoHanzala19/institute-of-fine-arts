using InstituteOfFineArts.Domain;

namespace InstituteOfFineArts.ViewModels;

public class ChartData
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Values { get; set; } = new();
}

public class HomeVm
{
    public Competition? Featured { get; set; }
    public List<Competition> Upcoming { get; set; } = new();
    public List<Submission> Paintings { get; set; } = new();
    public List<Award> RecentAwards { get; set; } = new();
    public List<Announcement> Announcements { get; set; } = new();
    public int Students { get; set; }
    public int Paintings_Count { get; set; }
    public int Competitions { get; set; }
    public int Awards { get; set; }
    public int Exhibitions { get; set; }
}

public class AdminDashboardVm
{
    public int Students { get; set; }
    public int Staff { get; set; }
    public int Managers { get; set; }
    public int ActiveCompetitions { get; set; }
    public int Submissions { get; set; }
    public int PendingReviews { get; set; }
    public int UnreadMessages { get; set; }
    public ChartData SubmissionsPerMonth { get; set; } = new();
    public ChartData GradeMix { get; set; } = new();
    public ChartData UsersByRole { get; set; } = new();
    public List<AuditLog> Recent { get; set; } = new();
    public List<Announcement> Announcements { get; set; } = new();
}

public class ManagerDashboardVm
{
    public int Students { get; set; }
    public int Staff { get; set; }
    public int Competitions { get; set; }
    public int Submissions { get; set; }
    public int Awards { get; set; }
    public int Exhibitions { get; set; }
    public int Listings { get; set; }
    public int PaintingsSold { get; set; }
    public decimal GrossSales { get; set; }
    public decimal Commission { get; set; }
    public decimal PayoutsDue { get; set; }
    public int PayoutsPendingCount { get; set; }
    public ChartData SalesByExhibition { get; set; } = new();
    public ChartData SubmissionsPerCompetition { get; set; } = new();
    public List<ExhibitionListing> RecentSales { get; set; } = new();
    public List<Exhibition> UpcomingExhibitions { get; set; } = new();
    public List<Announcement> Announcements { get; set; } = new();
}

public class StaffDashboardVm
{
    public string Name { get; set; } = string.Empty;
    public int TotalCompetitions { get; set; }
    public int Ongoing { get; set; }
    public int Submissions { get; set; }
    public int Pending { get; set; }
    public int Exhibitions { get; set; }
    public int AwardsIssued { get; set; }
    public List<Submission> Queue { get; set; } = new();
    public List<Competition> PastDeadline { get; set; } = new();
    public List<Competition> AwaitingAwards { get; set; } = new();
    public ChartData GradeMix { get; set; } = new();
    public List<Announcement> Announcements { get; set; } = new();
}

public class StudentDashboardVm
{
    public Student Student { get; set; } = null!;
    public List<Competition> Open { get; set; } = new();
    public List<Submission> Submissions { get; set; } = new();
    public List<Award> Awards { get; set; } = new();
    public List<ExhibitionListing> Listings { get; set; } = new();
    public List<Submission> Inspiration { get; set; } = new();
    public List<Announcement> Announcements { get; set; } = new();
    public int Evaluated { get; set; }
    public int Pending { get; set; }
    public decimal Earned { get; set; }
    public ChartData GradeMix { get; set; } = new();
}

public class SubmissionReportVm
{
    public int? CompetitionId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public SubmissionGrade? Grade { get; set; }
    public List<Submission> Rows { get; set; } = new();
    public List<Competition> Competitions { get; set; } = new();
    public ChartData GradeMix { get; set; } = new();
}

public class SalesReportVm
{
    public int? ExhibitionId { get; set; }
    public List<ExhibitionListing> Rows { get; set; } = new();
    public List<Exhibition> Exhibitions { get; set; } = new();
    public decimal Gross { get; set; }
    public decimal Commission { get; set; }
    public decimal NetToStudents { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }
}

public class CompetitionReportRow
{
    public Competition Competition { get; set; } = null!;
    public int Submissions { get; set; }
    public int Evaluated { get; set; }
    public int Best { get; set; }
    public int Disqualified { get; set; }
    public int Awards { get; set; }
    public decimal PrizeTotal { get; set; }
}
