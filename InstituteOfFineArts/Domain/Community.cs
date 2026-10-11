using System.ComponentModel.DataAnnotations;

namespace InstituteOfFineArts.Domain;

public class Announcement
{
    public int AnnouncementId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.Everyone;

    public bool IsPinned { get; set; }

    public DateTime PublishedAt { get; set; } = DateTime.Now;
    public DateTime? ExpiresAt { get; set; }

    public string? CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
}

public class Notification
{
    public int NotificationId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Message { get; set; }

    [StringLength(300)]
    public string? Url { get; set; }

    public NotificationType Type { get; set; } = NotificationType.Info;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ContactMessage
{
    public int ContactMessageId { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(3000)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class PaintingLike
{
    public int SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class AuditLog
{
    public long AuditLogId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    [StringLength(450)]
    public string? UserId { get; set; }

    [StringLength(150)]
    public string UserName { get; set; } = "system";

    [Required, StringLength(30)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string EntityName { get; set; } = string.Empty;

    [StringLength(60)]
    public string? EntityId { get; set; }

    [StringLength(2000)]
    public string? Details { get; set; }

    [StringLength(60)]
    public string? IpAddress { get; set; }
}
