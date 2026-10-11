using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace InstituteOfFineArts.Domain;

public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLoginAt { get; set; }

    public Staff? Staff { get; set; }
    public Student? Student { get; set; }
}

public class Staff
{
    public int StaffId { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    [Required, StringLength(20)]
    public string Gender { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string? EmergencyContact { get; set; }

    [StringLength(200)]
    public string? Qualification { get; set; }

    public DateTime DateJoined { get; set; }

    [StringLength(300)]
    public string? HandlesClass { get; set; }

    [StringLength(300)]
    public string? HandlesSubject { get; set; }

    [StringLength(100)]
    public string? Designation { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

public class Student
{
    public int StudentId { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    [Required, StringLength(20)]
    public string Gender { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ParentOrGuardianName { get; set; }

    [StringLength(20)]
    public string? ParentOrGuardianPhone { get; set; }

    [Required, StringLength(50)]
    public string EnrollmentNumber { get; set; } = string.Empty;

    public DateTime AdmissionDate { get; set; }

    [Required, StringLength(150)]
    public string Course { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ClassName { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<Award> Awards { get; set; } = new List<Award>();
}
