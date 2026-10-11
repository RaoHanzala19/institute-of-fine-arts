using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InstituteOfFineArts.Data;

/// <summary>
/// Applies migrations, creates the four roles and the first administrator, and – in demo mode –
/// fills the database with a believable season of competitions, paintings, awards and sales.
/// </summary>
public static class DbSeeder
{
    public const string AdminEmail = "admin@ifa.edu";
    public const string AdminPassword = "Admin@12345";

    public static async Task InitializeAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var options = sp.GetRequiredService<IOptions<InstituteOptions>>().Value;

        await db.Database.MigrateAsync();

        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));

        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (await users.FindByEmailAsync(AdminEmail) is null && !await db.Users.AnyAsync())
        {
            await CreateUserAsync(users, "Amina Rahman", AdminEmail, AdminPassword, Roles.Admin);
            log.LogInformation("Created default administrator {Email}", AdminEmail);
        }

        await StatusSyncService.SyncAsync(db);

        if (!options.SeedDemoData || await db.Competitions.AnyAsync()) return;

        log.LogInformation("Seeding demo data …");
        await new DemoData(db, users, sp.GetRequiredService<IImageService>()).RunAsync();
        log.LogInformation("Demo data ready.");
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> users, string fullName,
        string email, string password, string role)
    {
        var user = new ApplicationUser
        {
            UserName = email, Email = email, EmailConfirmed = true, FullName = fullName, IsActive = true,
            CreatedAt = DateTime.Now.AddDays(-200)
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not seed {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        await users.AddToRoleAsync(user, role);
        return user;
    }

    private sealed class DemoData
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IImageService _images;
        private readonly Random _rnd = new(2026);
        private int _paintingSeed = 11;

        public DemoData(AppDbContext db, UserManager<ApplicationUser> users, IImageService images)
        {
            _db = db; _users = users; _images = images;
        }

        private static readonly string[] Titles =
        {
            "Monsoon Over Clifton", "Reflections in Rain", "The Chai Vendor's Umbrella", "Grey Sky, Green Earth",
            "Kite Runner's Dusk", "Puddles of Gold", "The Last Rickshaw", "Whispering Banyan", "Harbour at Dawn",
            "Blue Hour Bazaar", "Threads of Saddar", "The Lantern Keeper", "Mosaic of Multan", "Where the River Sleeps",
            "Ochre Afternoons", "Jasmine Rooftop", "Echoes of Lahore", "Paper Boats", "Salt & Sunlight",
            "Midnight Market", "Fragments of Home", "Dust and Marigolds", "Sapphire Silence", "Cartography of Dreams",
            "Ink of the Old City", "Terrace Conversations", "Quiet Minaret", "The Weaver's Window", "Copper Sunrise",
            "A Thousand Windows", "Salt Pans at Dusk", "Golden Hour Ferry"
        };

        private static readonly (string Type, string Text)[] Texts =
        {
            ("Poem", "Rain writes softly on the rooftops,\nand the whole city learns to listen."),
            ("Quotation", "“Every artist dips his brush in his own soul, and paints his own nature into his pictures.” — Henry Ward Beecher"),
            ("Story", "My grandmother kept one blue tile from the old house. This painting is the wall it once belonged to, remembered from the outside in."),
            ("Description", "Layered acrylic washes over a charcoal underdrawing, built up slowly to catch the last light of the evening."),
            ("Poem", "Colour is a promise the morning keeps;\nwe only have to wake and hold the brush."),
            ("Description", "Oil on canvas. I wanted the crowd to feel like weather – always moving, never the same twice."),
            ("Quotation", "“Painting is silent poetry, and poetry is painting that speaks.” — Simonides"),
            ("Story", "Every evening the lantern-keeper climbs the same forty steps. I painted the forty-first, the one he never talks about.")
        };

        private static readonly string[] Positives =
        {
            "Confident use of colour with a clear, memorable focal point.",
            "Excellent composition — the eye travels through the picture naturally.",
            "A very personal voice; the accompanying text and image reinforce each other.",
            "Sensitive handling of light, especially in the mid-tones.",
            "Brushwork is expressive and controlled at the same time."
        };

        private static readonly string[] Negatives =
        {
            "The background is slightly busy and competes with the subject.",
            "Some edges feel unresolved in the lower third of the canvas.",
            "The palette drifts towards muddy greys in the shadows.",
            "Scale relationships between the foreground figures are inconsistent."
        };

        private static readonly string[] Improvements =
        {
            "Try a limited palette study before committing to the final piece.",
            "Push the contrast between the light and dark masses.",
            "Spend more time on the secondary shapes — they carry the composition.",
            "Experiment with a larger format to free up the brushwork."
        };

        public async Task RunAsync()
        {
            // ── people ────────────────────────────────────────────────────────
            var manager = await UserAsync("Rabia Hussain", "manager@ifa.edu", "Manager@12345", Roles.Manager);

            var staffSpecs = new[]
            {
                ("Sara", "Khan", "Female", "Head of Painting", "Oil & acrylic painting", "MFA, National College of Arts"),
                ("Bilal", "Ahmed", "Male", "Senior Instructor", "Drawing & composition", "BFA, Indus Valley School"),
                ("Ayesha", "Malik", "Female", "Art Historian", "History of art & criticism", "MA Art History, University of Karachi")
            };
            var staff = new List<Staff>();
            foreach (var (first, last, gender, designation, subject, qualification) in staffSpecs)
            {
                var u = await UserAsync($"{first} {last}", $"{first}.{last}@ifa.edu".ToLowerInvariant(), "Staff@12345", Roles.Staff);
                var s = new Staff
                {
                    ApplicationUserId = u.Id, FirstName = first, LastName = last, Gender = gender,
                    DateOfBirth = new DateTime(1980 + staff.Count * 3, 4 + staff.Count, 12), Address = "House 14, Block B, Gulshan-e-Iqbal, Karachi",
                    PhoneNumber = $"+92 300 55{staff.Count}0{staff.Count + 1}22", EmergencyContact = "+92 321 5550199",
                    Qualification = qualification, DateJoined = DateTime.Today.AddYears(-3 - staff.Count), HandlesClass = "Diploma Year 1, Year 2",
                    HandlesSubject = subject, Designation = designation, Remarks = "Seeded demo account."
                };
                _db.Staff.Add(s);
                staff.Add(s);
            }

            var studentSpecs = new[]
            {
                ("Ayesha", "Siddiqui", "Female", "Diploma in Fine Arts"), ("Hamza", "Qureshi", "Male", "Bachelor of Visual Arts"),
                ("Fatima", "Zahra", "Female", "Diploma in Fine Arts"), ("Usman", "Tariq", "Male", "Certificate in Oil Painting"),
                ("Zainab", "Ali", "Female", "Diploma in Illustration"), ("Danish", "Mehmood", "Male", "Bachelor of Visual Arts"),
                ("Hira", "Baig", "Female", "Diploma in Fine Arts"), ("Omar", "Farooq", "Male", "Diploma in Illustration"),
                ("Sana", "Javed", "Female", "Bachelor of Visual Arts"), ("Talha", "Rehman", "Male", "Certificate in Oil Painting"),
                ("Mariam", "Yousuf", "Female", "Diploma in Fine Arts"), ("Ibrahim", "Sheikh", "Male", "Bachelor of Visual Arts")
            };
            var students = new List<Student>();
            var i = 0;
            foreach (var (first, last, gender, course) in studentSpecs)
            {
                var u = await UserAsync($"{first} {last}", $"{first}.{last}@ifa.edu".ToLowerInvariant(), "Student@12345", Roles.Student);
                var s = new Student
                {
                    ApplicationUserId = u.Id, FirstName = first, LastName = last, Gender = gender,
                    DateOfBirth = new DateTime(2001 + i % 4, 1 + i % 12, 3 + i), Address = $"Flat {10 + i}, Clifton Block {1 + i % 9}, Karachi",
                    PhoneNumber = $"+92 333 40{i:D2}{100 + i}", ParentOrGuardianName = $"{(gender == "Female" ? "Mr." : "Mr.")} {last}",
                    ParentOrGuardianPhone = $"+92 300 90{i:D2}{200 + i}", EnrollmentNumber = $"IFA-2025-{i + 1:D3}",
                    AdmissionDate = new DateTime(2025, 8, 15).AddDays(i), Course = course, ClassName = i % 2 == 0 ? "Batch 2025-A" : "Batch 2025-B",
                    Remarks = i == 3 ? "Scholarship recipient." : null
                };
                _db.Students.Add(s);
                students.Add(s);
                i++;
            }
            await _db.SaveChangesAsync();

            var reviewer = staff[0].ApplicationUserId;
            var reviewer2 = staff[1].ApplicationUserId;
            var today = DateTime.Today;

            // ── competitions ──────────────────────────────────────────────────
            var monsoon = Competition("Monsoon Memories", "Nature & weather",
                "Capture how the monsoon changes a city, a street or a single quiet window. Any medium on canvas or paper is welcome, as long as the final work is photographed as a JPEG.",
                "One entry per student. Original work only. Minimum 300×300 px JPEG, maximum 10 MB. A poem, quotation, story or description must accompany the painting.",
                today.AddDays(-150), today.AddDays(-120).AddHours(23).AddMinutes(59), CompetitionStatus.Concluded, reviewer,
                (1, "First Prize", 50000m, "Gold medal, certificate and gallery feature"), (2, "Second Prize", 30000m, "Silver medal and certificate"), (3, "Third Prize", 15000m, "Bronze medal and certificate"));
            var portraits = Competition("Portraits of the City", "Portraiture",
                "Faces that make our city what it is — vendors, commuters, elders, children. Show us a person, not a pose.",
                "One entry per student. Original work only. Live sketches are welcome as accompanying material.",
                today.AddDays(-90), today.AddDays(-60).AddHours(23).AddMinutes(59), CompetitionStatus.Concluded, reviewer2,
                (1, "Grand Prize", 40000m, "Trophy and solo wall at the next showcase"), (2, "Runner-up", 20000m, "Certificate and art supplies hamper"));
            var heritage = Competition("Colours of Heritage", "Culture & heritage",
                "Interpret a monument, craft, festival or tradition that deserves to be remembered.",
                "One entry per student. Original work only. Entries after the deadline are disqualified automatically.",
                today.AddDays(-20), today.AddDays(12).AddHours(23).AddMinutes(59), CompetitionStatus.Ongoing, reviewer,
                (1, "First Prize", 45000m, "Trophy, certificate and exhibition placement"), (2, "Second Prize", 25000m, "Medal and certificate"), (3, "Third Prize", 10000m, "Certificate"));
            var abstractComp = Competition("Autumn Abstract", "Abstract",
                "Colour, rhythm, and emotion without a subject. Show us what a feeling looks like.",
                "One entry per student. Original work only.",
                today.AddDays(-30), today.AddDays(-2).AddHours(23).AddMinutes(59), CompetitionStatus.Ongoing, reviewer2,
                (1, "Best Abstract", 30000m, "Trophy and certificate"));
            var winter = Competition("Winter Light", "Light & shadow",
                "Low sun, long shadows, lamplit windows: paint the season's light.",
                "One entry per student. Original work only.",
                today.AddDays(25), today.AddDays(55).AddHours(23).AddMinutes(59), CompetitionStatus.Upcoming, reviewer,
                (1, "First Prize", 40000m, "Trophy and certificate"), (2, "Second Prize", 20000m, "Certificate"));
            var sketch = Competition("Young Masters Sketch-off", "Drawing",
                "A pencil, a page and a fresh idea. Drawings only — no colour.",
                "One entry per student. Original work only.",
                today.AddDays(60), today.AddDays(75).AddHours(23).AddMinutes(59), CompetitionStatus.Upcoming, reviewer2,
                (1, "Master Sketch", 25000m, "Certificate and drawing set"));

            _db.Competitions.AddRange(monsoon, portraits, heritage, abstractComp, winter, sketch);
            await _db.SaveChangesAsync();

            // ── submissions ───────────────────────────────────────────────────
            var monsoonSubs = await AddSubmissionsAsync(monsoon, students, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
                new[] { SubmissionGrade.Best, SubmissionGrade.Best, SubmissionGrade.Best, SubmissionGrade.Better, SubmissionGrade.Better,
                        SubmissionGrade.Good, SubmissionGrade.Good, SubmissionGrade.Moderate, SubmissionGrade.Normal, SubmissionGrade.Disqualified }, reviewer);
            var portraitSubs = await AddSubmissionsAsync(portraits, students, new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                new[] { SubmissionGrade.Best, SubmissionGrade.Best, SubmissionGrade.Better, SubmissionGrade.Better, SubmissionGrade.Good,
                        SubmissionGrade.Good, SubmissionGrade.Moderate, SubmissionGrade.Normal, SubmissionGrade.Normal }, reviewer2);
            await AddSubmissionsAsync(heritage, students, new[] { 0, 3, 5, 7, 9, 11 },
                new[] { SubmissionGrade.Better, SubmissionGrade.Good, SubmissionGrade.Pending, SubmissionGrade.Pending, SubmissionGrade.Pending, SubmissionGrade.Pending }, reviewer);
            await AddSubmissionsAsync(abstractComp, students, new[] { 1, 4, 6, 8, 10 },
                new[] { SubmissionGrade.Pending, SubmissionGrade.Pending, SubmissionGrade.Pending, SubmissionGrade.Pending, SubmissionGrade.Disqualified }, reviewer2, lastIsLate: true);

            // ── awards ────────────────────────────────────────────────────────
            var tiers = await _db.CompetitionAwards.ToListAsync();
            var awardNo = 1;
            void Award(Submission s, Competition c, int rank, string remarks)
            {
                var tier = tiers.First(t => t.CompetitionId == c.CompetitionId && t.Rank == rank);
                _db.Awards.Add(new Award
                {
                    CertificateNumber = $"IFA-{c.EndDate.Year}-{awardNo++:D4}", StudentId = s.StudentId, CompetitionId = c.CompetitionId,
                    SubmissionId = s.SubmissionId, CompetitionAwardId = tier.CompetitionAwardId, IssuedById = reviewer,
                    Remarks = remarks, DateIssued = c.EndDate.Date.AddDays(7)
                });
            }
            Award(monsoonSubs[0], monsoon, 1, "A luminous, technically assured piece that captured the mood of the competition.");
            Award(monsoonSubs[1], monsoon, 2, "Beautiful restraint and a very original point of view.");
            Award(monsoonSubs[2], monsoon, 3, "Warm, human and full of atmosphere.");
            Award(portraitSubs[0], portraits, 1, "A portrait with real presence — the sitter feels alive.");
            Award(portraitSubs[1], portraits, 2, "Sensitive drawing and a striking use of colour.");
            await _db.SaveChangesAsync();

            // ── exhibitions & sales ───────────────────────────────────────────
            var fair = new Exhibition
            {
                ExhibitionName = "Heritage Art Fair 2026", Venue = "Frere Hall Gallery", City = "Karachi", Country = "Pakistan",
                Description = "A weekend fair presenting the winning works of the season to collectors, alumni and the public.",
                ExhibitionStartDate = today.AddDays(-40), ExhibitionEndDate = today.AddDays(-30), CommissionPercent = 10m,
                Status = ExhibitionStatus.Concluded, CreatedById = manager.Id, AdditionalNotes = "Framing sponsored by Karachi Arts Council."
            };
            var showcase = new Exhibition
            {
                ExhibitionName = "Karachi Contemporary Showcase", Venue = "The Studio Space, DHA Phase 6", City = "Karachi", Country = "Pakistan",
                Description = "An evening-led showcase of young artists, with live sketching and a silent auction.",
                ExhibitionStartDate = today.AddDays(-3), ExhibitionEndDate = today.AddDays(4), CommissionPercent = 12m,
                Status = ExhibitionStatus.Ongoing, CreatedById = manager.Id
            };
            var lahore = new Exhibition
            {
                ExhibitionName = "Lahore Autumn Gallery", Venue = "Alhamra Arts Council", City = "Lahore", Country = "Pakistan",
                Description = "Our first exhibition outside Karachi, in partnership with Alhamra.",
                ExhibitionStartDate = today.AddDays(30), ExhibitionEndDate = today.AddDays(37), CommissionPercent = 10m,
                Status = ExhibitionStatus.Upcoming, CreatedById = manager.Id
            };
            _db.Exhibitions.AddRange(fair, showcase, lahore);
            await _db.SaveChangesAsync();

            var sold = new List<ExhibitionListing>();
            ExhibitionListing List(Exhibition e, Submission s, decimal ask, SaleStatus status = SaleStatus.Available,
                decimal? price = null, string? buyer = null, PaymentStatus pay = PaymentStatus.NotApplicable, bool fullyPaid = false)
            {
                var l = new ExhibitionListing
                {
                    ExhibitionId = e.ExhibitionId, SubmissionId = s.SubmissionId, ForwardedById = staff[0].ApplicationUserId,
                    StudentQuotedPrice = ask, SaleStatus = status, ListedAt = e.ExhibitionStartDate.AddDays(-10)
                };
                if (status == SaleStatus.Sold && price.HasValue)
                {
                    l.FinalSalePrice = price;
                    l.DateSold = e.ExhibitionStartDate.AddDays(2);
                    l.BuyerName = buyer; l.BuyerPhone = "+92 300 1234567"; l.BuyerEmail = "collector@example.com"; l.BuyerAddress = "Karachi";
                    l.CommissionAmount = Math.Round(price.Value * e.CommissionPercent / 100m, 2);
                    l.NetPayableToStudent = price.Value - l.CommissionAmount;
                    l.PaymentStatus = pay;
                    if (pay == PaymentStatus.Paid)
                    {
                        l.AmountPaidToStudent = fullyPaid ? l.NetPayableToStudent : l.NetPayableToStudent;
                        l.PaymentDate = l.DateSold.Value.AddDays(5);
                        l.PaymentRemarks = "Bank transfer";
                    }
                    sold.Add(l);
                }
                _db.ExhibitionListings.Add(l);
                return l;
            }

            List(fair, monsoonSubs[0], 60000m, SaleStatus.Sold, 72000m, "Mr. Kamran Lakhani", PaymentStatus.Paid, true);
            List(fair, monsoonSubs[1], 45000m, SaleStatus.Sold, 48500m, "Ms. Nadia Rizvi", PaymentStatus.Pending);
            List(fair, monsoonSubs[2], 30000m, SaleStatus.Returned);
            List(showcase, portraitSubs[0], 55000m, SaleStatus.Sold, 58000m, "Mr. Adeel Hashmi", PaymentStatus.Pending);
            List(showcase, portraitSubs[1], 38000m);
            List(lahore, monsoonSubs[0], 80000m);
            await _db.SaveChangesAsync();
            foreach (var l in sold) l.ReceiptNumber = $"RCP-{l.DateSold!.Value.Year}-{l.ExhibitionListingId:D5}";

            // ── community ─────────────────────────────────────────────────────
            _db.Announcements.AddRange(
                new Announcement
                {
                    Title = "Colours of Heritage closes in two weeks", IsPinned = true, Audience = AnnouncementAudience.Everyone, CreatedById = manager.Id,
                    Body = "Entries for Colours of Heritage close at 11:59 pm on the deadline shown on the competition page. Upload your JPEG early — late entries are disqualified automatically.",
                    PublishedAt = today.AddDays(-2)
                },
                new Announcement
                {
                    Title = "Evaluation week for Autumn Abstract", Audience = AnnouncementAudience.Staff, CreatedById = manager.Id,
                    Body = "Please clear the review queue for Autumn Abstract before Friday so we can conclude the competition and prepare the awards.",
                    PublishedAt = today.AddDays(-1)
                },
                new Announcement
                {
                    Title = "Studio hours extended", Audience = AnnouncementAudience.Students, CreatedById = manager.Id,
                    Body = "The painting studio stays open until 9 pm Monday to Thursday until the end of the month, so you can finish competition pieces on campus.",
                    PublishedAt = today.AddDays(-4), ExpiresAt = today.AddDays(20)
                });

            _db.ContactMessages.AddRange(
                new ContactMessage { Name = "Nadia Rizvi", Email = "nadia@example.com", Subject = "Enquiry about commissioned portraits", Message = "I bought a painting at the Heritage Art Fair and would love to commission a portrait from one of your students. Who should I speak to?", CreatedAt = DateTime.Now.AddHours(-9) },
                new ContactMessage { Name = "Asad Mirza", Email = "asad@example.com", Subject = "Admissions for next intake", Message = "Could you share the fee structure and admission dates for the Diploma in Fine Arts?", CreatedAt = DateTime.Now.AddDays(-2), IsRead = true },
                new ContactMessage { Name = "Karachi Arts Council", Email = "office@kac.example", Subject = "Framing sponsorship", Message = "We would be delighted to sponsor framing for the Lahore exhibition again.", CreatedAt = DateTime.Now.AddDays(-6), IsRead = true, IsResolved = true });

            foreach (var st in students.Take(5))
                _db.Notifications.Add(new Notification
                {
                    UserId = st.ApplicationUserId, Title = "Welcome to the Institute of Fine Arts portal",
                    Message = "Browse open competitions, upload your work and follow your feedback here.", Url = "/Student/Competitions", Type = NotificationType.Info,
                    CreatedAt = DateTime.Now.AddDays(-3)
                });

            await _db.SaveChangesAsync();

            // a handful of likes on the best paintings
            var bestIds = await _db.Submissions.Where(s => s.Grade == SubmissionGrade.Best).Select(s => s.SubmissionId).ToListAsync();
            foreach (var sid in bestIds)
                foreach (var st in students.OrderBy(_ => _rnd.Next()).Take(_rnd.Next(1, 7)))
                    _db.PaintingLikes.Add(new PaintingLike { SubmissionId = sid, UserId = st.ApplicationUserId });
            await _db.SaveChangesAsync();

            // the seed itself should not flood the audit trail
            await _db.AuditLogs.ExecuteDeleteAsync();
            _db.AuditLogs.Add(new AuditLog { Action = "Seeded", EntityName = "Database", Details = "Demo data created on first start-up." });
            await _db.SaveChangesAsync();
        }

        private async Task<ApplicationUser> UserAsync(string fullName, string email, string password, string role)
            => await CreateUserAsync(_users, fullName, email, password, role);

        private static Competition Competition(string title, string theme, string description, string conditions,
            DateTime start, DateTime end, CompetitionStatus status, string createdBy,
            params (int Rank, string Title, decimal Prize, string Description)[] tiers)
        {
            var c = new Competition
            {
                Title = title, Theme = theme, Description = description, Conditions = conditions, StartDate = start,
                EndDate = end, Status = status, CreatedById = createdBy, CreatedAt = start.AddDays(-14)
            };
            foreach (var t in tiers)
                c.CompetitionAwards.Add(new CompetitionAward { Rank = t.Rank, AwardTitle = t.Title, PrizeMoney = t.Prize, AwardDescription = t.Description });
            return c;
        }

        private async Task<List<Submission>> AddSubmissionsAsync(Competition c, List<Student> students, int[] studentIndexes,
            SubmissionGrade[] grades, string evaluatorId, bool lastIsLate = false)
        {
            var list = new List<Submission>();
            for (var k = 0; k < studentIndexes.Length; k++)
            {
                var student = students[studentIndexes[k]];
                var (type, text) = Texts[(k + c.CompetitionId) % Texts.Length];
                var late = lastIsLate && k == studentIndexes.Length - 1;
                var img = _images.SaveBitmap(PaintingGenerator.Generate(_paintingSeed++, 1000, 680));
                var window = Math.Max(1, (int)(c.EndDate - c.StartDate).TotalDays - 1);
                var submittedAt = late ? c.EndDate.AddDays(1).AddHours(2)
                    : c.StartDate.AddDays(_rnd.Next(1, window)).AddHours(_rnd.Next(8, 22));

                var s = new Submission
                {
                    StudentId = student.StudentId, CompetitionId = c.CompetitionId,
                    PaintingTitle = Titles[(_paintingSeed + k) % Titles.Length],
                    ImageFilePath = img.Path, ThumbnailPath = img.ThumbPath, OriginalFileName = $"painting-{k + 1}.jpg",
                    FileSizeInBytes = img.Size, ImageWidth = img.Width, ImageHeight = img.Height,
                    AccompanyingText = text, TextType = type,
                    CreativeDescription = k % 2 == 0 ? "Painted over three weekends using a mix of acrylic and gouache." : null,
                    SubmittedAt = submittedAt, IsLateSubmission = late, Grade = grades[k]
                };

                if (grades[k] != SubmissionGrade.Pending)
                {
                    s.Evaluation = new Evaluation
                    {
                        EvaluatedById = evaluatorId, Grade = grades[k], EvaluatedAt = c.EndDate.AddDays(_rnd.Next(2, 6)),
                        PositivePoints = grades[k] == SubmissionGrade.Disqualified ? null : Positives[_rnd.Next(Positives.Length)],
                        NegativePoints = grades[k] is SubmissionGrade.Best or SubmissionGrade.Disqualified ? null : Negatives[_rnd.Next(Negatives.Length)],
                        AreasOfImprovement = grades[k] is SubmissionGrade.Best or SubmissionGrade.Disqualified ? null : Improvements[_rnd.Next(Improvements.Length)],
                        GeneralRemarks = grades[k] switch
                        {
                            SubmissionGrade.Best => "Outstanding work — one of the strongest entries this season.",
                            SubmissionGrade.Better => "A very accomplished piece with clear promise.",
                            SubmissionGrade.Good => "Solid work; keep building on the strengths noted above.",
                            SubmissionGrade.Disqualified => late ? "Submitted after the deadline, so the entry is disqualified under the competition rules."
                                                                  : "Disqualified: the artwork closely resembles an existing published image.",
                            _ => "Reasonable effort — see the suggestions for next time."
                        }
                    };
                }

                _db.Submissions.Add(s);
                list.Add(s);
            }
            await _db.SaveChangesAsync();
            return list;
        }
    }
}
