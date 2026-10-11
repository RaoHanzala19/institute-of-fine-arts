using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Services;
using InstituteOfFineArts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InstituteOfFineArts.Controllers;

public class StudentDetailsVm
{
    public Student Student { get; set; } = null!;
    public List<Submission> Submissions { get; set; } = new();
    public List<Award> Awards { get; set; } = new();
    public List<ExhibitionListing> Listings { get; set; } = new();
}

public class UserRow
{
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public int? StaffId { get; set; }
    public int? StudentId { get; set; }
}

// ══ Students ══════════════════════════════════════════════════════════════════
[Authorize(Roles = Roles.Internal)]
public class StudentsController : PortalController
{
    private readonly AccountProvisioner _accounts;

    public StudentsController(AppDbContext db, AccountProvisioner accounts) : base(db) => _accounts = accounts;

    public async Task<IActionResult> Index(string? q, string? course, bool? active, int page = 1, bool export = false)
    {
        var query = Db.Students.AsNoTracking().Include(s => s.ApplicationUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.FirstName.Contains(q) || s.LastName.Contains(q) ||
                                     s.EnrollmentNumber.Contains(q) || s.ApplicationUser.Email!.Contains(q));
        if (!string.IsNullOrWhiteSpace(course)) query = query.Where(s => s.Course == course);
        if (active.HasValue) query = query.Where(s => s.IsActive == active);
        query = query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName);

        if (export)
        {
            var all = await query.ToListAsync();
            return Csv.File("students",
                new[] { "Enrollment", "First name", "Last name", "Email", "Phone", "Course", "Class", "Admitted", "Active" },
                all.Select(s => new object?[] { s.EnrollmentNumber, s.FirstName, s.LastName, s.ApplicationUser.Email, s.PhoneNumber, s.Course, s.ClassName, s.AdmissionDate, s.IsActive ? "Yes" : "No" }));
        }

        ViewBag.Q = q; ViewBag.Course = course; ViewBag.Active = active;
        ViewBag.Courses = new SelectList(await Db.Students.Select(s => s.Course).Distinct().OrderBy(c => c).ToListAsync(), course);
        return View(await PagedList<Student>.CreateAsync(query, page, 12));
    }

    public async Task<IActionResult> Details(int id)
    {
        var s = await Db.Students.AsNoTracking().Include(x => x.ApplicationUser).FirstOrDefaultAsync(x => x.StudentId == id);
        if (s is null) return NotFound();
        return View(new StudentDetailsVm
        {
            Student = s,
            Submissions = await Db.Submissions.AsNoTracking().Include(x => x.Competition).Where(x => x.StudentId == id).OrderByDescending(x => x.SubmittedAt).ToListAsync(),
            Awards = await Db.Awards.AsNoTracking().Include(a => a.Competition).Include(a => a.CompetitionAward).Where(a => a.StudentId == id).OrderByDescending(a => a.DateIssued).ToListAsync(),
            Listings = await Db.ExhibitionListings.AsNoTracking().Include(l => l.Exhibition).Include(l => l.Submission).Where(l => l.Submission.StudentId == id).ToListAsync()
        });
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create()
    {
        var year = DateTime.Now.Year;
        var next = await Db.Students.CountAsync() + 1;
        return View("Form", new StudentFormVm { EnrollmentNumber = $"IFA-{year}-{next:D3}" });
    }

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(StudentFormVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Email)) ModelState.AddModelError(nameof(vm.Email), "Email is required.");
        if (string.IsNullOrWhiteSpace(vm.Password)) ModelState.AddModelError(nameof(vm.Password), "A starting password is required.");
        if (await Db.Students.AnyAsync(s => s.EnrollmentNumber == vm.EnrollmentNumber))
            ModelState.AddModelError(nameof(vm.EnrollmentNumber), "This enrollment number is already in use.");
        CheckDates(vm.DateOfBirth, vm.AdmissionDate, ModelState);
        if (!ModelState.IsValid) return View("Form", vm);

        var (user, errors) = await _accounts.CreateAsync($"{vm.FirstName} {vm.LastName}", vm.Email, vm.Password!, Roles.Student);
        if (user is null)
        {
            foreach (var e in errors) ModelState.AddModelError("", e);
            return View("Form", vm);
        }

        try
        {
            Db.Students.Add(new Student
            {
                ApplicationUserId = user.Id, FirstName = vm.FirstName.Trim(), LastName = vm.LastName.Trim(), Gender = vm.Gender,
                DateOfBirth = vm.DateOfBirth!.Value, Address = vm.Address, PhoneNumber = vm.PhoneNumber,
                ParentOrGuardianName = vm.ParentOrGuardianName, ParentOrGuardianPhone = vm.ParentOrGuardianPhone,
                EnrollmentNumber = vm.EnrollmentNumber.Trim(), AdmissionDate = vm.AdmissionDate!.Value,
                Course = vm.Course.Trim(), ClassName = vm.ClassName, Remarks = vm.Remarks
            });
            await Db.SaveChangesAsync();
        }
        catch
        {
            await _accounts.RollbackAsync(user);
            throw;
        }

        Success($"{vm.FirstName} {vm.LastName} has been enrolled.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await Db.Students.AsNoTracking().Include(x => x.ApplicationUser).FirstOrDefaultAsync(x => x.StudentId == id);
        if (s is null) return NotFound();
        return View("Form", new StudentFormVm
        {
            StudentId = s.StudentId, FirstName = s.FirstName, LastName = s.LastName, Email = s.ApplicationUser.Email ?? "",
            Gender = s.Gender, DateOfBirth = s.DateOfBirth, Address = s.Address, PhoneNumber = s.PhoneNumber,
            ParentOrGuardianName = s.ParentOrGuardianName, ParentOrGuardianPhone = s.ParentOrGuardianPhone,
            EnrollmentNumber = s.EnrollmentNumber, AdmissionDate = s.AdmissionDate, Course = s.Course,
            ClassName = s.ClassName, Remarks = s.Remarks, IsActive = s.IsActive
        });
    }

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id, StudentFormVm vm)
    {
        if (id != vm.StudentId) return BadRequest();
        ModelState.Remove(nameof(vm.Email));
        ModelState.Remove(nameof(vm.Password));
        var s = await Db.Students.Include(x => x.ApplicationUser).FirstOrDefaultAsync(x => x.StudentId == id);
        if (s is null) return NotFound();
        vm.Email = s.ApplicationUser.Email ?? "";

        if (await Db.Students.AnyAsync(x => x.EnrollmentNumber == vm.EnrollmentNumber && x.StudentId != id))
            ModelState.AddModelError(nameof(vm.EnrollmentNumber), "This enrollment number is already in use.");
        CheckDates(vm.DateOfBirth, vm.AdmissionDate, ModelState);
        if (!ModelState.IsValid) return View("Form", vm);

        s.FirstName = vm.FirstName.Trim(); s.LastName = vm.LastName.Trim(); s.Gender = vm.Gender;
        s.DateOfBirth = vm.DateOfBirth!.Value; s.Address = vm.Address; s.PhoneNumber = vm.PhoneNumber;
        s.ParentOrGuardianName = vm.ParentOrGuardianName; s.ParentOrGuardianPhone = vm.ParentOrGuardianPhone;
        s.EnrollmentNumber = vm.EnrollmentNumber.Trim(); s.AdmissionDate = vm.AdmissionDate!.Value;
        s.Course = vm.Course.Trim(); s.ClassName = vm.ClassName; s.Remarks = vm.Remarks;
        s.UpdatedAt = DateTime.Now;
        s.ApplicationUser.FullName = s.FullName;
        await Db.SaveChangesAsync();

        if (s.IsActive != vm.IsActive)
        {
            s.IsActive = vm.IsActive;
            await Db.SaveChangesAsync();
            await _accounts.SetActiveAsync(s.ApplicationUserId, vm.IsActive);
        }

        Success("Student record updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var s = await Db.Students.FirstOrDefaultAsync(x => x.StudentId == id);
        if (s is null) return NotFound();
        s.IsActive = !s.IsActive;
        await Db.SaveChangesAsync();
        await _accounts.SetActiveAsync(s.ApplicationUserId, s.IsActive);
        Success(s.IsActive ? $"{s.FullName} is active again." : $"{s.FullName} has been deactivated and signed out.");
        return RedirectToAction(nameof(Details), new { id });
    }

    internal static void CheckDates(DateTime? dob, DateTime? joined, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ms)
    {
        if (dob.HasValue && (dob > DateTime.Today || dob < new DateTime(1930, 1, 1)))
            ms.AddModelError("DateOfBirth", "Please enter a realistic date of birth.");
        if (joined.HasValue && joined > DateTime.Today.AddDays(1))
            ms.AddModelError("AdmissionDate", "The admission date cannot be in the future.");
    }
}

// ══ Staff members ═════════════════════════════════════════════════════════════
[Authorize(Roles = Roles.Internal)]
public class StaffMembersController : PortalController
{
    private readonly AccountProvisioner _accounts;

    public StaffMembersController(AppDbContext db, AccountProvisioner accounts) : base(db) => _accounts = accounts;

    public async Task<IActionResult> Index(string? q, bool? active, int page = 1, bool export = false)
    {
        var query = Db.Staff.AsNoTracking().Include(s => s.ApplicationUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.FirstName.Contains(q) || s.LastName.Contains(q) ||
                                     s.ApplicationUser.Email!.Contains(q) || (s.Designation != null && s.Designation.Contains(q)));
        if (active.HasValue) query = query.Where(s => s.IsActive == active);
        query = query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName);

        if (export)
        {
            var all = await query.ToListAsync();
            return Csv.File("staff",
                new[] { "First name", "Last name", "Email", "Phone", "Designation", "Subjects", "Joined", "Active" },
                all.Select(s => new object?[] { s.FirstName, s.LastName, s.ApplicationUser.Email, s.PhoneNumber, s.Designation, s.HandlesSubject, s.DateJoined, s.IsActive ? "Yes" : "No" }));
        }

        ViewBag.Q = q; ViewBag.Active = active;
        return View(await PagedList<Staff>.CreateAsync(query, page, 12));
    }

    public async Task<IActionResult> Details(int id)
    {
        var s = await Db.Staff.AsNoTracking().Include(x => x.ApplicationUser).FirstOrDefaultAsync(x => x.StaffId == id);
        if (s is null) return NotFound();
        ViewBag.Evaluations = await Db.Evaluations.CountAsync(e => e.EvaluatedById == s.ApplicationUserId);
        ViewBag.Competitions = await Db.Competitions.CountAsync(c => c.CreatedById == s.ApplicationUserId);
        ViewBag.Awards = await Db.Awards.CountAsync(a => a.IssuedById == s.ApplicationUserId);
        return View(s);
    }

    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create() => View("Form", new StaffFormVm());

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(StaffFormVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Email)) ModelState.AddModelError(nameof(vm.Email), "Email is required.");
        if (string.IsNullOrWhiteSpace(vm.Password)) ModelState.AddModelError(nameof(vm.Password), "A starting password is required.");
        if (vm.DateOfBirth is { } dob && (dob > DateTime.Today.AddYears(-16) || dob < new DateTime(1930, 1, 1)))
            ModelState.AddModelError(nameof(vm.DateOfBirth), "Staff members must be at least 16 years old.");
        if (!ModelState.IsValid) return View("Form", vm);

        var (user, errors) = await _accounts.CreateAsync($"{vm.FirstName} {vm.LastName}", vm.Email, vm.Password!, Roles.Staff);
        if (user is null)
        {
            foreach (var e in errors) ModelState.AddModelError("", e);
            return View("Form", vm);
        }

        try
        {
            Db.Staff.Add(new Staff
            {
                ApplicationUserId = user.Id, FirstName = vm.FirstName.Trim(), LastName = vm.LastName.Trim(), Gender = vm.Gender,
                DateOfBirth = vm.DateOfBirth!.Value, Address = vm.Address, PhoneNumber = vm.PhoneNumber,
                EmergencyContact = vm.EmergencyContact, Qualification = vm.Qualification, DateJoined = vm.DateJoined!.Value,
                HandlesClass = vm.HandlesClass, HandlesSubject = vm.HandlesSubject, Designation = vm.Designation, Remarks = vm.Remarks
            });
            await Db.SaveChangesAsync();
        }
        catch
        {
            await _accounts.RollbackAsync(user);
            throw;
        }

        Success($"{vm.FirstName} {vm.LastName} joined the staff.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await Db.Staff.AsNoTracking().Include(x => x.ApplicationUser).FirstOrDefaultAsync(x => x.StaffId == id);
        if (s is null) return NotFound();
        return View("Form", new StaffFormVm
        {
            StaffId = s.StaffId, FirstName = s.FirstName, LastName = s.LastName, Email = s.ApplicationUser.Email ?? "",
            Gender = s.Gender, DateOfBirth = s.DateOfBirth, Address = s.Address, PhoneNumber = s.PhoneNumber,
            EmergencyContact = s.EmergencyContact, Qualification = s.Qualification, DateJoined = s.DateJoined,
            HandlesClass = s.HandlesClass, HandlesSubject = s.HandlesSubject, Designation = s.Designation,
            Remarks = s.Remarks, IsActive = s.IsActive
        });
    }

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id, StaffFormVm vm)
    {
        if (id != vm.StaffId) return BadRequest();
        ModelState.Remove(nameof(vm.Email));
        ModelState.Remove(nameof(vm.Password));
        var s = await Db.Staff.Include(x => x.ApplicationUser).FirstOrDefaultAsync(x => x.StaffId == id);
        if (s is null) return NotFound();
        vm.Email = s.ApplicationUser.Email ?? "";
        if (!ModelState.IsValid) return View("Form", vm);

        s.FirstName = vm.FirstName.Trim(); s.LastName = vm.LastName.Trim(); s.Gender = vm.Gender;
        s.DateOfBirth = vm.DateOfBirth!.Value; s.Address = vm.Address; s.PhoneNumber = vm.PhoneNumber;
        s.EmergencyContact = vm.EmergencyContact; s.Qualification = vm.Qualification; s.DateJoined = vm.DateJoined!.Value;
        s.HandlesClass = vm.HandlesClass; s.HandlesSubject = vm.HandlesSubject; s.Designation = vm.Designation;
        s.Remarks = vm.Remarks; s.UpdatedAt = DateTime.Now;
        s.ApplicationUser.FullName = s.FullName;
        await Db.SaveChangesAsync();

        if (s.IsActive != vm.IsActive)
        {
            s.IsActive = vm.IsActive;
            await Db.SaveChangesAsync();
            await _accounts.SetActiveAsync(s.ApplicationUserId, vm.IsActive);
        }

        Success("Staff record updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var s = await Db.Staff.FirstOrDefaultAsync(x => x.StaffId == id);
        if (s is null) return NotFound();
        s.IsActive = !s.IsActive;
        await Db.SaveChangesAsync();
        await _accounts.SetActiveAsync(s.ApplicationUserId, s.IsActive);
        Success(s.IsActive ? $"{s.FullName} is active again." : $"{s.FullName} has been deactivated and signed out.");
        return RedirectToAction(nameof(Details), new { id });
    }
}

// ══ All accounts (admin) ══════════════════════════════════════════════════════
[Authorize(Roles = Roles.Admin)]
public class UsersController : PortalController
{
    private readonly AccountProvisioner _accounts;

    public UsersController(AppDbContext db, AccountProvisioner accounts) : base(db) => _accounts = accounts;

    public async Task<IActionResult> Index(string? role, string? q)
    {
        var query = from u in Db.Users.AsNoTracking()
                    join ur in Db.UserRoles on u.Id equals ur.UserId
                    join r in Db.Roles on ur.RoleId equals r.Id
                    select new UserRow
                    {
                        User = u, Role = r.Name!,
                        StaffId = u.Staff != null ? u.Staff.StaffId : (int?)null,
                        StudentId = u.Student != null ? u.Student.StudentId : (int?)null
                    };

        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(x => x.Role == role);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.User.FullName.Contains(q) || x.User.Email!.Contains(q));

        ViewBag.Role = role; ViewBag.Q = q;
        return View(await query.OrderBy(x => x.Role).ThenBy(x => x.User.FullName).ToListAsync());
    }

    public IActionResult Create() => View(new UserFormVm());

    [HttpPost]
    public async Task<IActionResult> Create(UserFormVm vm)
    {
        if (vm.Role is not (Roles.Admin or Roles.Manager))
            ModelState.AddModelError(nameof(vm.Role), "Only Admin and Manager accounts are created here — enrol staff and students from their own screens.");
        if (!ModelState.IsValid) return View(vm);

        var (user, errors) = await _accounts.CreateAsync(vm.FullName, vm.Email, vm.Password, vm.Role);
        if (user is null)
        {
            foreach (var e in errors) ModelState.AddModelError("", e);
            return View(vm);
        }

        Db.AddAudit("Created", "Account", user.Id, $"{user.Email} ({vm.Role})");
        await Db.SaveChangesAsync();
        Success($"{vm.Role} account created for {vm.FullName}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(string id)
    {
        if (id == UserId)
        {
            Error("You cannot deactivate your own account.");
            return RedirectToAction(nameof(Index));
        }

        var user = await Db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var isAdmin = await (from ur in Db.UserRoles join r in Db.Roles on ur.RoleId equals r.Id
                             where ur.UserId == id && r.Name == Roles.Admin select ur).AnyAsync();
        if (isAdmin && user.IsActive && await _accounts.ActiveAdminCountAsync() <= 1)
        {
            Error("At least one active administrator must remain.");
            return RedirectToAction(nameof(Index));
        }

        await _accounts.SetActiveAsync(id, !user.IsActive);
        // keep profile rows in step
        var staff = await Db.Staff.FirstOrDefaultAsync(s => s.ApplicationUserId == id);
        var student = await Db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == id);
        if (staff != null) staff.IsActive = !user.IsActive;
        if (student != null) student.IsActive = !user.IsActive;
        Db.AddAudit(user.IsActive ? "Deactivated" : "Activated", "Account", id, user.Email);
        await Db.SaveChangesAsync();

        Success(user.IsActive ? "Account deactivated." : "Account reactivated.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ResetPassword(string id, string? returnUrl)
    {
        var user = await Db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        return View(new ResetPasswordVm { UserId = id, DisplayName = user.FullName, ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var errors = await _accounts.ResetPasswordAsync(vm.UserId, vm.NewPassword);
        if (errors.Count > 0)
        {
            foreach (var e in errors) ModelState.AddModelError("", e);
            return View(vm);
        }

        Db.AddAudit("PasswordReset", "Account", vm.UserId, vm.DisplayName);
        await Db.SaveChangesAsync();
        Success($"Password reset for {vm.DisplayName}.");
        return ToReturnUrl(vm.ReturnUrl, nameof(Index));
    }
}
