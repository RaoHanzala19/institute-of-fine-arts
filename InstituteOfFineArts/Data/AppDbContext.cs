using System.Text;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Infrastructure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace InstituteOfFineArts.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<CompetitionAward> CompetitionAwards => Set<CompetitionAward>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Evaluation> Evaluations => Set<Evaluation>();
    public DbSet<Award> Awards => Set<Award>();
    public DbSet<Exhibition> Exhibitions => Set<Exhibition>();
    public DbSet<ExhibitionListing> ExhibitionListings => Set<ExhibitionListing>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<PaintingLike> PaintingLikes => Set<PaintingLike>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<decimal?>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // People -------------------------------------------------------------------
        b.Entity<Staff>()
            .HasOne(s => s.ApplicationUser).WithOne(u => u.Staff)
            .HasForeignKey<Staff>(s => s.ApplicationUserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Student>()
            .HasOne(s => s.ApplicationUser).WithOne(u => u.Student)
            .HasForeignKey<Student>(s => s.ApplicationUserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Student>().HasIndex(s => s.EnrollmentNumber).IsUnique();

        // Competitions -------------------------------------------------------------
        b.Entity<Competition>()
            .HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Competition>().HasIndex(c => c.Status);

        b.Entity<CompetitionAward>()
            .HasOne(ca => ca.Competition).WithMany(c => c.CompetitionAwards)
            .HasForeignKey(ca => ca.CompetitionId).OnDelete(DeleteBehavior.Cascade);

        // Submissions --------------------------------------------------------------
        b.Entity<Submission>()
            .HasOne(s => s.Student).WithMany(st => st.Submissions)
            .HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Submission>()
            .HasOne(s => s.Competition).WithMany(c => c.Submissions)
            .HasForeignKey(s => s.CompetitionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Submission>().HasIndex(s => new { s.StudentId, s.CompetitionId }).IsUnique();
        b.Entity<Submission>().HasIndex(s => s.Grade);

        b.Entity<Evaluation>()
            .HasOne(e => e.Submission).WithOne(s => s.Evaluation)
            .HasForeignKey<Evaluation>(e => e.SubmissionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Evaluation>()
            .HasOne(e => e.EvaluatedBy).WithMany().HasForeignKey(e => e.EvaluatedById).OnDelete(DeleteBehavior.Restrict);

        // Awards -------------------------------------------------------------------
        b.Entity<Award>()
            .HasOne(a => a.Student).WithMany(s => s.Awards)
            .HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Award>()
            .HasOne(a => a.Competition).WithMany(c => c.IssuedAwards)
            .HasForeignKey(a => a.CompetitionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Award>()
            .HasOne(a => a.CompetitionAward).WithMany(ca => ca.IssuedAwards)
            .HasForeignKey(a => a.CompetitionAwardId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Award>()
            .HasOne(a => a.WinningSubmission).WithMany()
            .HasForeignKey(a => a.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Award>()
            .HasOne(a => a.IssuedBy).WithMany().HasForeignKey(a => a.IssuedById).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Award>().HasIndex(a => a.SubmissionId).IsUnique();
        b.Entity<Award>().HasIndex(a => a.CertificateNumber).IsUnique();
        b.Entity<Award>().HasIndex(a => new { a.CompetitionId, a.CompetitionAwardId })
            .IsUnique().HasFilter("[CompetitionAwardId] IS NOT NULL");

        // Exhibitions --------------------------------------------------------------
        b.Entity<Exhibition>()
            .HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);

        b.Entity<ExhibitionListing>()
            .HasOne(el => el.Exhibition).WithMany(e => e.ExhibitionListings)
            .HasForeignKey(el => el.ExhibitionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ExhibitionListing>()
            .HasOne(el => el.Submission).WithMany(s => s.ExhibitionListings)
            .HasForeignKey(el => el.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ExhibitionListing>()
            .HasOne(el => el.ForwardedBy).WithMany().HasForeignKey(el => el.ForwardedById).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ExhibitionListing>().HasIndex(el => new { el.ExhibitionId, el.SubmissionId }).IsUnique();

        // Community ----------------------------------------------------------------
        b.Entity<Announcement>()
            .HasOne(a => a.CreatedBy).WithMany().HasForeignKey(a => a.CreatedById).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Notification>()
            .HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead });

        b.Entity<PaintingLike>().HasKey(l => new { l.SubmissionId, l.UserId });
        b.Entity<PaintingLike>()
            .HasOne(l => l.Submission).WithMany(s => s.Likes)
            .HasForeignKey(l => l.SubmissionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PaintingLike>()
            .HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<AuditLog>().HasIndex(a => a.Timestamp);
    }

    // ── Audit trail ────────────────────────────────────────────────────────────
    private static readonly HashSet<Type> Audited = new()
    {
        typeof(Competition), typeof(CompetitionAward), typeof(Submission), typeof(Evaluation),
        typeof(Award), typeof(Exhibition), typeof(ExhibitionListing), typeof(Student),
        typeof(Staff), typeof(Announcement)
    };

    private static readonly HashSet<string> IgnoredProperties = new() { "UpdatedAt", "LastUpdatedAt", "CreatedAt" };

    /// <summary>Queues a hand-written audit entry (login, password reset, …). Caller saves.</summary>
    public void AddAudit(string action, string entity, string? entityId = null, string? details = null,
        string? userId = null, string? userName = null)
    {
        var log = NewLog(action, entity, entityId, details);
        if (userId != null) log.UserId = userId;      // e.g. sign-in, where the principal is not set yet
        if (userName != null) log.UserName = userName;
        AuditLogs.Add(log);
    }

    private AuditLog NewLog(string action, string entity, string? id, string? details) => new()
    {
        Action = action,
        EntityName = entity,
        EntityId = id,
        Details = details is { Length: > 2000 } ? details[..2000] : details,
        UserId = _currentUser?.UserId,
        UserName = _currentUser?.UserName ?? "system",
        IpAddress = _currentUser?.IpAddress
    };

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var pending = new List<(EntityEntry Entry, AuditLog Log)>();

        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => Audited.Contains(e.Entity.GetType()) &&
                                 e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                     .ToList())
        {
            var name = entry.Entity.GetType().Name;
            switch (entry.State)
            {
                case EntityState.Added:
                    pending.Add((entry, NewLog("Created", name, null, Describe(entry))));
                    break;
                case EntityState.Deleted:
                    pending.Add((entry, NewLog("Deleted", name, KeyOf(entry), Describe(entry))));
                    break;
                case EntityState.Modified:
                    var sb = new StringBuilder();
                    foreach (var p in entry.Properties.Where(p => p.IsModified && !IgnoredProperties.Contains(p.Metadata.Name)))
                    {
                        if (Equals(p.OriginalValue, p.CurrentValue)) continue;
                        sb.Append($"{p.Metadata.Name}: {Trim(p.OriginalValue)} → {Trim(p.CurrentValue)}; ");
                    }
                    if (sb.Length > 0)
                        pending.Add((entry, NewLog("Updated", name, KeyOf(entry), sb.ToString().TrimEnd())));
                    break;
            }
        }

        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        var extra = false;
        foreach (var (entry, log) in pending)
        {
            log.EntityId ??= KeyOf(entry);
            AuditLogs.Add(log);
            extra = true;
        }

        // Hand-queued audit rows are already part of the first save; only flush the ones we just added.
        if (extra)
            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        return result;
    }

    private static string? KeyOf(EntityEntry entry) =>
        entry.Metadata.FindPrimaryKey()?.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .FirstOrDefault();

    private static string Describe(EntityEntry entry)
    {
        var interesting = entry.Properties
            .Where(p => p.Metadata.ClrType == typeof(string) && p.CurrentValue is string { Length: > 0 } &&
                        (p.Metadata.Name.Contains("Title") || p.Metadata.Name.Contains("Name") ||
                         p.Metadata.Name.Contains("Number")))
            .Take(3)
            .Select(p => $"{p.Metadata.Name}={Trim(p.CurrentValue)}");
        return string.Join(", ", interesting);
    }

    private static string Trim(object? v)
    {
        var s = v?.ToString() ?? "∅";
        return s.Length > 60 ? s[..57] + "…" : s;
    }
}
