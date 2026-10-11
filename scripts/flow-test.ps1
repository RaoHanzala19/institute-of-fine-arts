<#
  End-to-end business-rule test. Drives the running app with curl and verifies the results in SQL Server.
  Usage:  ./scripts/flow-test.ps1 -BaseUrl http://localhost:5210
  Requires the app to be running with the freshly seeded demo data (it CHANGES data, run once per fresh database).
#>
param(
    [string]$BaseUrl = "http://localhost:5210",
    [string]$Server = ".\MSSQLSERVER_22",
    [string]$Database = "InstituteOfFineArtsV2"
)
Add-Type -AssemblyName System.Drawing
$tmp = Join-Path $env:TEMP "ifa-flow"; New-Item -ItemType Directory -Force $tmp | Out-Null
$script:pass = 0; $script:fail = 0

function Sql([string]$q) {
    $r = & sqlcmd -S $Server -E -C -d $Database -h -1 -W -Q "SET NOCOUNT ON; $q" 2>&1
    return (($r | Out-String).Trim())
}
function Assert($name, $cond, $detail = "") {
    if ($cond) { $script:pass++; Write-Host "  ok   $name" -ForegroundColor DarkGreen }
    else { $script:fail++; Write-Host "  FAIL $name  $detail" -ForegroundColor Red }
}
function Token([string]$html) { if ($html -match 'name="__RequestVerificationToken"[^>]*value="([^"]+)"') { $Matches[1] } else { $null } }
function Get-Page($jar, $path) { (& curl.exe -s -b $jar -c $jar "$BaseUrl$path") -join "`n" }

function Login($email, $password) {
    $jar = Join-Path $tmp ("jar-" + [guid]::NewGuid().ToString("N") + ".txt")
    $tok = Token (Get-Page $jar "/Account/Login")
    $code = & curl.exe -s -o NUL -w "%{http_code}" -b $jar -c $jar --data-urlencode "Email=$email" --data-urlencode "Password=$password" --data-urlencode "__RequestVerificationToken=$tok" "$BaseUrl/Account/Login"
    return @{ Jar = $jar; Code = $code }
}

# POST a form. $formPage supplies the antiforgery token; fields may include a file (multipart).
function Post($jar, $formPage, $postPath, [hashtable]$fields, $fileField = $null, $file = $null) {
    $tok = Token (Get-Page $jar $formPage)
    $out = Join-Path $tmp "out.html"
    $args = @("-s", "-o", $out, "-w", "%{http_code}|%{redirect_url}", "-b", $jar, "-c", $jar)
    if ($file) {
        foreach ($k in $fields.Keys) { $args += @("-F", "$k=$($fields[$k])") }
        $args += @("-F", "__RequestVerificationToken=$tok", "-F", "${fileField}=@$file;type=image/jpeg")
    } else {
        foreach ($k in $fields.Keys) { $args += @("--data-urlencode", "$k=$($fields[$k])") }
        $args += @("--data-urlencode", "__RequestVerificationToken=$tok")
    }
    $args += "$BaseUrl$postPath"
    $res = (& curl.exe @args) -join ""
    $parts = $res -split "\|", 2
    return @{ Code = [int]$parts[0]; Redirect = $parts[1]; Body = (Get-Content $out -Raw -ErrorAction SilentlyContinue) }
}

# ── fixtures ───────────────────────────────────────────────────────────────────
$jpeg = Join-Path $tmp "painting.jpg"
$bmp = New-Object Drawing.Bitmap 900, 600; $g = [Drawing.Graphics]::FromImage($bmp)
$g.Clear([Drawing.Color]::Coral); $g.FillEllipse([Drawing.Brushes]::Navy, 120, 90, 420, 320); $g.FillRectangle([Drawing.Brushes]::Gold, 500, 300, 300, 200)
$bmp.Save($jpeg, [Drawing.Imaging.ImageFormat]::Jpeg); $g.Dispose(); $bmp.Dispose()
$fake = Join-Path $tmp "fake.jpg"; Set-Content $fake "this is definitely not an image"
$png = Join-Path $tmp "real.png"; $b2 = New-Object Drawing.Bitmap 500, 500; $b2.Save($png, [Drawing.Imaging.ImageFormat]::Png); $b2.Dispose()

$compHeritage = Sql "SELECT CompetitionId FROM Competitions WHERE Title='Colours of Heritage'"
$compAbstract = Sql "SELECT CompetitionId FROM Competitions WHERE Title='Autumn Abstract'"
$compPortraits = Sql "SELECT CompetitionId FROM Competitions WHERE Title='Portraits of the City'"
$compWinter = Sql "SELECT CompetitionId FROM Competitions WHERE Title='Winter Light'"

# ── 1. Student submissions ─────────────────────────────────────────────────────
Write-Host "`n== Student uploads ==" -ForegroundColor Cyan
$hamza = Login "hamza.qureshi@ifa.edu" "Student@12345"
Assert "student can sign in" ($hamza.Code -eq "302") "code $($hamza.Code)"

$r = Post $hamza.Jar "/Student/Upload" "/Student/Upload" @{ CompetitionId = $compHeritage; PaintingTitle = "Flow Test Sunrise"; TextType = "Poem"; AccompanyingText = "A short poem about the morning light." } "PaintingFile" $jpeg
Assert "valid JPEG upload redirects to the entry" ($r.Code -eq 302 -and $r.Redirect -like "*SubmissionDetails*") "code $($r.Code) $($r.Redirect)"
$subA = Sql "SELECT TOP 1 SubmissionId FROM Submissions WHERE PaintingTitle='Flow Test Sunrise'"
Assert "submission stored as Pending, not late" ((Sql "SELECT CAST(Grade AS varchar)+','+CAST(IsLateSubmission AS varchar) FROM Submissions WHERE SubmissionId=$subA") -eq "0,0")
$path = Sql "SELECT ImageFilePath FROM Submissions WHERE SubmissionId=$subA"
$root = "C:\Users\hp\Desktop\InstituteOfFineArts\InstituteOfFineArts-v2\InstituteOfFineArts\wwwroot"
Assert "original file and thumbnail exist on disk" ((Test-Path ($root + $path.Replace('/', '\'))) -and (Test-Path ($root + ($path -replace 'paintings', 'thumbs').Replace('/', '\'))))

$r = Post $hamza.Jar "/Dashboard" "/Student/Upload" @{ CompetitionId = $compHeritage; PaintingTitle = "Second try"; TextType = "Story"; AccompanyingText = "Trying to enter twice." } "PaintingFile" $jpeg
Assert "second entry in the same competition is rejected" ($r.Code -eq 200 -and $r.Body -match "already entered")

$r = Post $hamza.Jar "/Dashboard" "/Student/Upload" @{ CompetitionId = $compWinter; PaintingTitle = "Too early"; TextType = "Story"; AccompanyingText = "Competition has not opened yet." } "PaintingFile" $jpeg
Assert "entry before the competition opens is rejected" ($r.Code -eq 200 -and $r.Body -match "not opened yet|not open")

$fatima = Login "fatima.zahra@ifa.edu" "Student@12345"
$r = Post $fatima.Jar "/Student/Upload" "/Student/Upload" @{ CompetitionId = $compAbstract; PaintingTitle = "Fake File"; TextType = "Description"; AccompanyingText = "Uploading a text file renamed to jpg."; AcceptLate = "true" } "PaintingFile" $fake
Assert "text file renamed to .jpg is rejected by content check" ($r.Code -eq 200 -and $r.Body -match "not a valid JPEG")

$r = Post $fatima.Jar "/Student/Upload" "/Student/Upload" @{ CompetitionId = $compAbstract; PaintingTitle = "Late Bloom"; TextType = "Description"; AccompanyingText = "Uploaded after the deadline on purpose." } "PaintingFile" $jpeg
Assert "late upload without confirmation is rejected" ($r.Code -eq 200 -and $r.Body -match "deadline has passed")

$r = Post $fatima.Jar "/Student/Upload" "/Student/Upload" @{ CompetitionId = $compAbstract; PaintingTitle = "Late Bloom"; TextType = "Description"; AccompanyingText = "Uploaded after the deadline on purpose."; AcceptLate = "true" } "PaintingFile" $jpeg
$subLate = Sql "SELECT TOP 1 SubmissionId FROM Submissions WHERE PaintingTitle='Late Bloom'"
Assert "confirmed late upload is stored and auto-disqualified" ($r.Code -eq 302 -and (Sql "SELECT CAST(Grade AS varchar)+','+CAST(IsLateSubmission AS varchar) FROM Submissions WHERE SubmissionId=$subLate") -eq "6,1")

$r = Post $fatima.Jar "/Student/SubmissionDetails/$subLate" "/Student/Edit/$subLate" @{ SubmissionId = $subLate; CompetitionId = $compAbstract; PaintingTitle = "Edited after deadline"; TextType = "Poem"; AccompanyingText = "Trying to edit after the deadline." }
Assert "editing after the deadline is blocked" ((Sql "SELECT PaintingTitle FROM Submissions WHERE SubmissionId=$subLate") -eq "Late Bloom")

$r = Post $hamza.Jar "/Student/SubmissionDetails/$subA" "/Student/Edit/$subA" @{ SubmissionId = $subA; CompetitionId = $compHeritage; PaintingTitle = "Flow Test Sunrise (edited)"; TextType = "Poem"; AccompanyingText = "An edited poem about the morning light." }
Assert "editing before the deadline works" ($r.Code -eq 302 -and (Sql "SELECT PaintingTitle FROM Submissions WHERE SubmissionId=$subA") -eq "Flow Test Sunrise (edited)")

# ── 2. Evaluation ──────────────────────────────────────────────────────────────
Write-Host "`n== Evaluation ==" -ForegroundColor Cyan
$staff = Login "sara.khan@ifa.edu" "Staff@12345"
$r = Post $staff.Jar "/Submissions/Evaluate/$subLate" "/Submissions/Evaluate/$subLate" @{ SubmissionId = $subLate; Grade = "Best"; GeneralRemarks = "trying to rescue a late entry" }
Assert "late entry cannot be graded above Disqualified" ((Sql "SELECT Grade FROM Submissions WHERE SubmissionId=$subLate") -eq "6")

$r = Post $staff.Jar "/Submissions/Evaluate/$subA" "/Submissions/Evaluate/$subA" @{ SubmissionId = $subA; Grade = "Good"; PositivePoints = "Nice colour."; NegativePoints = "Busy edges."; AreasOfImprovement = "Simplify."; GeneralRemarks = "Solid." }
Assert "staff can evaluate a pending painting" ($r.Code -eq 302 -and (Sql "SELECT Grade FROM Submissions WHERE SubmissionId=$subA") -eq "3")
Assert "student receives a notification about the grade" ((Sql "SELECT COUNT(*) FROM Notifications n JOIN Students s ON s.ApplicationUserId=n.UserId WHERE s.FirstName='Hamza' AND n.Title LIKE '%Flow Test Sunrise%graded%'") -ge 1)

$mgr = Login "manager@ifa.edu" "Manager@12345"
$r = Post $mgr.Jar "/Submissions/Details/$subA" "/Submissions/Evaluate/$subA" @{ SubmissionId = $subA; Grade = "Best" }
Assert "manager cannot evaluate (read-only role)" ((Sql "SELECT Grade FROM Submissions WHERE SubmissionId=$subA") -eq "3")

# ── 3. Awards ──────────────────────────────────────────────────────────────────
Write-Host "`n== Awards ==" -ForegroundColor Cyan
$betterInPortraits = Sql "SELECT TOP 1 s.SubmissionId FROM Submissions s WHERE s.CompetitionId=$compPortraits AND s.Grade=2 ORDER BY s.SubmissionId"
$tier1 = Sql "SELECT CompetitionAwardId FROM CompetitionAwards WHERE CompetitionId=$compPortraits AND Rank=1"
$r = Post $staff.Jar "/Awards/Create" "/Awards/Create" @{ CompetitionId = $compPortraits; SubmissionId = $betterInPortraits; CompetitionAwardId = $tier1; DateIssued = (Get-Date -Format "yyyy-MM-dd") }
Assert "a tier that was already awarded cannot be given twice" ($r.Code -eq 200 -and $r.Body -match "already been given")

$r = Post $staff.Jar "/Awards/Create" "/Awards/Create" @{ CompetitionId = $compHeritage; SubmissionId = $subA; CustomAwardTitle = "Too early"; DateIssued = (Get-Date -Format "yyyy-MM-dd") }
Assert "awards cannot be issued before the competition is concluded" ($r.Code -eq 200 -and $r.Body -match "concluded")

$before = [int](Sql "SELECT COUNT(*) FROM Awards")
$r = Post $staff.Jar "/Awards/Create" "/Awards/Create" @{ CompetitionId = $compPortraits; SubmissionId = $betterInPortraits; CustomAwardTitle = "Jury's Special Mention"; DateIssued = (Get-Date -Format "yyyy-MM-dd") }
Assert "a custom award is issued with a certificate number" ($r.Code -eq 302 -and [int](Sql "SELECT COUNT(*) FROM Awards") -eq ($before + 1) -and (Sql "SELECT TOP 1 CertificateNumber FROM Awards ORDER BY AwardId DESC") -match "^IFA-\d{4}-\d{4}$")

$r = Post $staff.Jar "/Awards/Create" "/Awards/Create" @{ CompetitionId = $compPortraits; SubmissionId = $betterInPortraits; CustomAwardTitle = "Duplicate"; DateIssued = (Get-Date -Format "yyyy-MM-dd") }
Assert "one painting cannot win two awards" ($r.Code -eq 200 -and $r.Body -match "already has an award")

# ── 4. Competition lifecycle ───────────────────────────────────────────────────
Write-Host "`n== Competition lifecycle ==" -ForegroundColor Cyan
$r = Post $staff.Jar "/Competitions/Create" "/Competitions/Create" @{ Title = "Flow Test Contest"; Theme = "Testing"; Description = "Created by the flow test."; StartDate = (Get-Date).AddHours(-1).ToString("yyyy-MM-ddTHH:mm"); EndDate = (Get-Date).AddDays(5).ToString("yyyy-MM-ddTHH:mm"); "Awards[0].Rank" = "1"; "Awards[0].AwardTitle" = "Winner"; "Awards[0].PrizeMoney" = "1000" }
$newComp = Sql "SELECT TOP 1 CompetitionId FROM Competitions WHERE Title='Flow Test Contest'"
Assert "staff can create a competition; it opens immediately when the start date has passed" ($r.Code -eq 302 -and (Sql "SELECT Status FROM Competitions WHERE CompetitionId=$newComp") -eq "2")
Assert "students are notified about the new competition" ((Sql "SELECT COUNT(*) FROM Notifications WHERE Title LIKE 'New competition: Flow Test Contest'") -ge 12)

$r = Post $staff.Jar "/Competitions/Create" "/Competitions/Create" @{ Title = "Bad dates"; StartDate = (Get-Date).AddDays(3).ToString("yyyy-MM-ddTHH:mm"); EndDate = (Get-Date).AddDays(1).ToString("yyyy-MM-ddTHH:mm") }
Assert "deadline before the opening date is rejected" ($r.Code -eq 200 -and $r.Body -match "deadline must be after")

$r = Post $staff.Jar "/Competitions/Details/$compHeritage" "/Competitions/Delete/$compHeritage" @{}
Assert "a competition that has entries cannot be deleted" ((Sql "SELECT COUNT(*) FROM Competitions WHERE CompetitionId=$compHeritage") -eq "1")

$r = Post $staff.Jar "/Competitions/Details/$newComp" "/Competitions/Conclude/$newComp" @{}
Assert "staff can conclude a competition" ((Sql "SELECT Status FROM Competitions WHERE CompetitionId=$newComp") -eq "3")

# ── 5. Exhibition sale & commission ────────────────────────────────────────────
Write-Host "`n== Exhibitions, sales & commission ==" -ForegroundColor Cyan
$listing = Sql "SELECT TOP 1 l.ExhibitionListingId FROM ExhibitionListings l JOIN Exhibitions e ON e.ExhibitionId=l.ExhibitionId WHERE e.ExhibitionName='Karachi Contemporary Showcase' AND l.SaleStatus=1"
$r = Post $mgr.Jar "/Listings/Sale/$listing" "/Listings/Sale/$listing" @{ ExhibitionListingId = $listing; SaleStatus = "Sold"; FinalSalePrice = "40000"; DateSold = (Get-Date -Format "yyyy-MM-dd"); BuyerName = ""; PaymentStatus = "Pending" }
Assert "a sale without a buyer name is rejected" ($r.Code -eq 200 -and $r.Body -match "buyer")

$r = Post $mgr.Jar "/Listings/Sale/$listing" "/Listings/Sale/$listing" @{ ExhibitionListingId = $listing; SaleStatus = "Sold"; FinalSalePrice = "40000"; DateSold = (Get-Date -Format "yyyy-MM-dd"); BuyerName = "Flow Buyer"; PaymentStatus = "Pending" }
Assert "manager can record a sale" ($r.Code -eq 302)
Assert "12% commission is calculated (4800) and the student's net is 35200" ((Sql "SELECT CAST(CommissionAmount AS varchar)+'|'+CAST(NetPayableToStudent AS varchar) FROM ExhibitionListings WHERE ExhibitionListingId=$listing") -eq "4800.00|35200.00")
Assert "a receipt number is generated" ((Sql "SELECT ReceiptNumber FROM ExhibitionListings WHERE ExhibitionListingId=$listing") -match "^RCP-\d{4}-\d{5}$")

$r = Post $mgr.Jar "/Listings/Sale/$listing" "/Listings/Sale/$listing" @{ ExhibitionListingId = $listing; SaleStatus = "Sold"; FinalSalePrice = "40000"; DateSold = (Get-Date -Format "yyyy-MM-dd"); BuyerName = "Flow Buyer"; PaymentStatus = "Paid"; AmountPaidToStudent = "39000"; PaymentDate = (Get-Date -Format "yyyy-MM-dd") }
Assert "paying the student more than their net share is rejected" ($r.Code -eq 200 -and $r.Body -match "owed at most")

$r = Post $mgr.Jar "/Listings/Sale/$listing" "/Listings/Sale/$listing" @{ ExhibitionListingId = $listing; SaleStatus = "Sold"; FinalSalePrice = "40000"; DateSold = (Get-Date -Format "yyyy-MM-dd"); BuyerName = "Flow Buyer"; PaymentStatus = "Paid"; AmountPaidToStudent = "35200"; PaymentDate = (Get-Date -Format "yyyy-MM-dd") }
Assert "the exact payout marks the listing Paid" ($r.Code -eq 302 -and (Sql "SELECT PaymentStatus FROM ExhibitionListings WHERE ExhibitionListingId=$listing") -eq "2")

$r = Post $mgr.Jar "/Listings/Sale/$listing" "/Listings/Delete/$listing" @{}
Assert "a sold painting cannot be removed from the records" ((Sql "SELECT COUNT(*) FROM ExhibitionListings WHERE ExhibitionListingId=$listing") -eq "1")

$notBest = Sql "SELECT TOP 1 SubmissionId FROM Submissions WHERE Grade=3 AND SubmissionId<>$subA"
$expo = Sql "SELECT ExhibitionId FROM Exhibitions WHERE ExhibitionName='Lahore Autumn Gallery'"
$r = Post $staff.Jar "/Listings/Create" "/Listings/Create" @{ ExhibitionId = $expo; SubmissionId = $notBest; StudentQuotedPrice = "5000" }
Assert "only Best-graded paintings can be forwarded to an exhibition" ($r.Code -eq 200 -and $r.Body -match "graded Best")

# ── 6. Accounts ────────────────────────────────────────────────────────────────
Write-Host "`n== Accounts & security ==" -ForegroundColor Cyan
$admin = Login "admin@ifa.edu" "Admin@12345"
$stuFields = @{ FirstName = "Noor"; LastName = "Tester"; Email = "noor.tester@ifa.edu"; Password = "Weak"; Gender = "Female"; DateOfBirth = "2004-05-05"; Address = "Karachi"; PhoneNumber = "+92 300 1112233"; EnrollmentNumber = "IFA-9-001"; AdmissionDate = (Get-Date -Format "yyyy-MM-dd"); Course = "Diploma in Fine Arts" }
$r = Post $admin.Jar "/Students/Create" "/Students/Create" $stuFields
Assert "weak starting password is refused" ($r.Code -eq 200 -and (Sql "SELECT COUNT(*) FROM AspNetUsers WHERE Email='noor.tester@ifa.edu'") -eq "0")

$stuFields.Password = "Noor@12345"
$r = Post $admin.Jar "/Students/Create" "/Students/Create" $stuFields
Assert "admin can enrol a student (account + profile)" ($r.Code -eq 302 -and (Sql "SELECT COUNT(*) FROM Students WHERE EnrollmentNumber='IFA-9-001'") -eq "1")

$r = Post $admin.Jar "/Students/Create" "/Students/Create" $stuFields
Assert "duplicate enrollment number / email is refused" ($r.Code -eq 200 -and (Sql "SELECT COUNT(*) FROM Students WHERE EnrollmentNumber='IFA-9-001'") -eq "1")

$noor = Login "noor.tester@ifa.edu" "Noor@12345"
Assert "the new student can sign in" ($noor.Code -eq "302")
$code = & curl.exe -s -o NUL -w "%{http_code}" -b $noor.Jar "$BaseUrl/Student/Competitions"
Assert "and reaches the student portal" ($code -eq "200")

$noorId = Sql "SELECT StudentId FROM Students WHERE EnrollmentNumber='IFA-9-001'"
$r = Post $admin.Jar "/Students/Details/$noorId" "/Students/ToggleActive/$noorId" @{}
Assert "deactivating a student succeeds" ((Sql "SELECT IsActive FROM Students WHERE StudentId=$noorId") -eq "0")
$final = & curl.exe -s -o NUL -w "%{url_effective}" -L -b $noor.Jar "$BaseUrl/Student/Competitions"
Assert "an already signed-in deactivated student is signed out on the next request" ($final -match "Account/Login")
$again = Login "noor.tester@ifa.edu" "Noor@12345"
Assert "and cannot sign in again" ($again.Code -eq "200")

$bad = Login "admin@ifa.edu" "wrong-password"
Assert "wrong password does not sign in" ($bad.Code -eq "200")

# ── 7. Public features ────────────────────────────────────────────────────────
Write-Host "`n== Public features ==" -ForegroundColor Cyan
$anon = Join-Path $tmp "anon.txt"
$r = Post $anon "/Home/Contact" "/Home/Contact" @{ Name = "Flow Tester"; Email = "flow@example.com"; Subject = "Hello from the flow test"; Message = "This is a message sent by the automated flow test." }
Assert "the public contact form stores a message" ($r.Code -eq 302 -and (Sql "SELECT COUNT(*) FROM ContactMessages WHERE Subject='Hello from the flow test'") -eq "1")

$tok = (Get-Page $hamza.Jar "/Gallery") -match 'name="csrf-token" content="([^"]+)"'; $csrf = $Matches[1]
$likeJson = (& curl.exe -s -X POST -H "RequestVerificationToken: $csrf" -b $hamza.Jar -c $hamza.Jar "$BaseUrl/Gallery/Like/1") -join ""
Assert "a signed-in user can like a Best painting via AJAX" ($likeJson -match '"liked":(true|false)') $likeJson
$likeAnon = & curl.exe -s -o NUL -w "%{http_code}" -X POST "$BaseUrl/Gallery/Like/1"
Assert "anonymous like attempts are rejected" ($likeAnon -ne "200")

$auditRows = [int](Sql "SELECT COUNT(*) FROM AuditLogs")
Assert "the audit trail recorded the changes made above (found $auditRows rows)" ($auditRows -ge 15)

Write-Host ("`nResult: {0} passed, {1} failed" -f $script:pass, $script:fail) -ForegroundColor $(if ($script:fail -eq 0) { "Green" } else { "Red" })
if ($script:fail -gt 0) { exit 1 }
