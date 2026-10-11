<#
  Smoke test: signs in as every demo role and requests every page.
  Usage:  ./scripts/smoke-test.ps1 -BaseUrl http://localhost:5210
  Requires the app to be running with the seeded demo data.
#>
param([string]$BaseUrl = "http://localhost:5210")

$ErrorActionPreference = "Stop"
$script:pass = 0; $script:fail = 0

function New-Session { $s = New-Object Microsoft.PowerShell.Commands.WebRequestSession; $s }

function Get-Token($html) {
    if ($html -match 'name="__RequestVerificationToken"[^>]*value="([^"]+)"') { return $Matches[1] }
    if ($html -match 'value="([^"]+)"[^>]*name="__RequestVerificationToken"') { return $Matches[1] }
    return $null
}

function Sign-In($email, $password) {
    $s = New-Session
    $page = Invoke-WebRequest "$BaseUrl/Account/Login" -WebSession $s -UseBasicParsing
    $token = Get-Token $page.Content
    $body = @{ Email = $email; Password = $password; RememberMe = "false"; __RequestVerificationToken = $token }
    $r = Invoke-WebRequest "$BaseUrl/Account/Login" -Method Post -Body $body -WebSession $s -UseBasicParsing -MaximumRedirection 5
    if ($r.BaseResponse.ResponseUri.AbsolutePath -like "/Account/Login*") { throw "Sign-in failed for $email" }
    return $s
}

function Check($session, [string]$path, [string]$expect = "ok") {
    $status = 0; $final = ""; $body = ""
    try {
        $r = Invoke-WebRequest "$BaseUrl$path" -WebSession $session -UseBasicParsing -MaximumRedirection 5
        $status = [int]$r.StatusCode; $final = $r.BaseResponse.ResponseUri.AbsolutePath; $body = $r.Content
    } catch {
        $resp = $_.Exception.Response
        if ($resp) { $status = [int]$resp.StatusCode; try { $sr = New-Object IO.StreamReader($resp.GetResponseStream()); $body = $sr.ReadToEnd() } catch {} }
        else { $status = -1; $body = $_.Exception.Message }
    }
    $good = switch ($expect) {
        "ok"       { $status -eq 200 -and $final -notlike "/Account/AccessDenied*" -and ($final -notlike "/Account/Login*" -or $path -like "/Account/Login*") }
        "denied"   { $final -like "/Account/AccessDenied*" -or $final -like "/Account/Login*" -or $status -eq 403 }
        "notfound" { $status -eq 404 }
        default    { $false }
    }
    if ($good) { $script:pass++; Write-Host ("  ok   {0,-9} {1}" -f $expect, $path) -ForegroundColor DarkGreen }
    else {
        $script:fail++
        $snippet = ($body -replace '<[^>]+>', ' ' -replace '\s+', ' ')
        if ($snippet.Length -gt 260) { $snippet = $snippet.Substring(0, 260) }
        Write-Host ("  FAIL {0,-9} {1}  -> HTTP {2} {3}" -f $expect, $path, $status, $final) -ForegroundColor Red
        Write-Host "       $snippet" -ForegroundColor DarkYellow
    }
}

Write-Host "`n== Anonymous ==" -ForegroundColor Cyan
$anon = New-Session
"/", "/Home/About", "/Home/FAQ", "/Home/Contact", "/Gallery", "/Gallery?sort=liked", "/Gallery/Details/1", "/Account/Login", "/health" | ForEach-Object { Check $anon $_ }
Check $anon "/Home/Status?code=404" "notfound"
Check $anon "/Gallery/Details/9999" "notfound"
"/Dashboard", "/Competitions", "/Student/Competitions", "/Users" | ForEach-Object { Check $anon $_ "denied" }

Write-Host "`n== Admin ==" -ForegroundColor Cyan
$admin = Sign-In "admin@ifa.edu" "Admin@12345"
$adminPaths = @(
    "/Dashboard", "/Competitions", "/Competitions?status=Ongoing", "/Competitions/Details/1", "/Competitions/Create", "/Competitions/Edit/1",
    "/Submissions", "/Submissions?grade=Best", "/Submissions/Details/1", "/Submissions/Evaluate/1",
    "/Awards", "/Awards/Details/1", "/Awards/Create", "/Awards/Edit/1", "/Awards/Candidates?competitionId=1", "/Certificates/Certificate/1",
    "/Exhibitions", "/Exhibitions/Details/1", "/Exhibitions/Create", "/Exhibitions/Edit/1",
    "/Listings", "/Listings/Create", "/Listings/Sale/1", "/Listings/Receipt/1",
    "/Students", "/Students/Details/1", "/Students/Create", "/Students/Edit/1",
    "/StaffMembers", "/StaffMembers/Details/1", "/StaffMembers/Create", "/StaffMembers/Edit/1",
    "/Users", "/Users/Create",
    "/Announcements", "/Announcements/Create", "/Announcements/Edit/1", "/Notifications", "/Messages", "/Messages/Details/1", "/Audit",
    "/Reports", "/Reports/Submissions", "/Reports/Sales", "/Reports/Competitions",
    "/Account/Profile", "/Account/ChangePassword",
    "/Competitions?export=true", "/Students?export=true", "/Submissions?export=true", "/Listings?export=true", "/Reports/Sales?export=true", "/Audit?export=true"
)
$adminPaths | ForEach-Object { Check $admin $_ }
Check $admin "/Competitions/Details/9999" "notfound"
Check $admin "/Student/Competitions" "denied"

Write-Host "`n== Manager ==" -ForegroundColor Cyan
$mgr = Sign-In "manager@ifa.edu" "Manager@12345"
"/Dashboard", "/Competitions", "/Competitions/Details/1", "/Submissions", "/Awards", "/Exhibitions", "/Exhibitions/Create", "/Listings", "/Listings/Sale/4", "/Students", "/Students/Details/2", "/StaffMembers",
"/Reports/Sales", "/Announcements", "/Messages", "/Account/Profile" | ForEach-Object { Check $mgr $_ }
"/Competitions/Create", "/Submissions/Evaluate/1", "/Awards/Create", "/Students/Create", "/Users", "/Audit", "/Student/Competitions" | ForEach-Object { Check $mgr $_ "denied" }

Write-Host "`n== Staff ==" -ForegroundColor Cyan
$staff = Sign-In "sara.khan@ifa.edu" "Staff@12345"
"/Dashboard", "/Competitions", "/Competitions/Create", "/Competitions/Edit/3", "/Submissions", "/Submissions/Evaluate/12", "/Awards", "/Awards/Create", "/Exhibitions", "/Listings", "/Listings/Create",
"/Students", "/StaffMembers", "/Reports/Submissions", "/Notifications" | ForEach-Object { Check $staff $_ }
"/Users", "/Audit", "/Announcements", "/Messages", "/Students/Create", "/Student/Upload" | ForEach-Object { Check $staff $_ "denied" }

Write-Host "`n== Student ==" -ForegroundColor Cyan
$stu = Sign-In "ayesha.siddiqui@ifa.edu" "Student@12345"
"/Dashboard", "/Student/Competitions", "/Student/Competitions?tab=past", "/Student/Details/3", "/Student/Details/1", "/Student/Upload", "/Student/Submissions", "/Student/SubmissionDetails/1", "/Student/Feedback",
"/Student/Awards", "/Student/Exhibitions", "/Certificates/Certificate/1", "/Notifications", "/Account/Profile", "/Gallery", "/Gallery/Details/1" | ForEach-Object { Check $stu $_ }
"/Competitions", "/Submissions", "/Students", "/Reports", "/Users", "/Listings" | ForEach-Object { Check $stu $_ "denied" }
Check $stu "/Certificates/Certificate/4" "denied"     # someone else's certificate
Check $stu "/Student/SubmissionDetails/2" "notfound"  # someone else's painting

Write-Host ("`nResult: {0} passed, {1} failed" -f $script:pass, $script:fail) -ForegroundColor $(if ($script:fail -eq 0) { "Green" } else { "Red" })
if ($script:fail -gt 0) { exit 1 }
