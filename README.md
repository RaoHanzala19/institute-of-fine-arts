# Institute of Fine Arts — Portal (v2)

A ground-up rebuild of the Institute of Fine Arts system: painting **competitions**, JPEG **submissions**, instructor
**evaluations**, **awards** with printable certificates, **exhibitions** with sales, commission and student payouts —
for four roles (Admin, Manager, Staff, Student) plus a public website and gallery.

**Stack:** ASP.NET Core 8 MVC · ASP.NET Core Identity · Entity Framework Core 8 · **SQL Server** · SkiaSharp (image validation & thumbnails) · Bootstrap 5 + a custom "Atelier" theme (light & dark) · Chart.js

> The original project in `../InstituteOfFineArts` is untouched. This version uses its own database
> (`InstituteOfFineArtsV2`) so nothing of yours is overwritten.

---

## Run it

Prerequisites: **.NET 8 SDK (or newer)** and a SQL Server instance.

```bash
cd InstituteOfFineArts-v2
dotnet run --project InstituteOfFineArts --urls http://localhost:5210
```

On first start the app **creates the database, applies the EF Core migration and seeds demo data**
(a full season of competitions, 30 generated JPEG paintings, evaluations, awards, exhibitions and sales).
Or open `InstituteOfFineArts.sln` in Visual Studio and press F5.

### Connection string

`InstituteOfFineArts/appsettings.json` → `ConnectionStrings:DefaultConnection`
(default: `Server=.\MSSQLSERVER_22;Database=InstituteOfFineArtsV2;Trusted_Connection=True;...`).
Change the server name to yours, or switch to SQL authentication — an example is in the file's comment.
A plain SQL script of the schema is in [`Database/schema.sql`](Database/schema.sql).

Set `"Institute": { "SeedDemoData": false }` to start with only the four roles and one administrator.

### Demo accounts

| Role    | Email                        | Password         |
|---------|------------------------------|------------------|
| Admin   | `admin@ifa.edu`              | `Admin@12345`    |
| Manager | `manager@ifa.edu`            | `Manager@12345`  |
| Staff   | `sara.khan@ifa.edu`          | `Staff@12345`    |
| Student | `ayesha.siddiqui@ifa.edu`    | `Student@12345`  |

The login page (in Development) has click-to-fill buttons. **Change these passwords before any real deployment.**

---

## What each role can do

| Area | Admin | Manager | Staff | Student |
|---|:-:|:-:|:-:|:-:|
| Create staff / student / manager accounts, deactivate, reset passwords | ✅ | | | |
| View students & staff (search, filter, CSV export) | ✅ | ✅ | ✅ | |
| Create / edit / conclude competitions & award tiers | ✅ | 👁 | ✅ | |
| Enter competitions, upload / edit / withdraw JPEG paintings | | | | ✅ |
| Evaluate paintings (grade + positive / negative / improvement / remarks) | ✅ | 👁 | ✅ | sees own |
| Issue awards + printable certificates | ✅ | 👁 | ✅ | sees own |
| Create exhibitions, forward *Best* paintings, record sales & payouts, receipts | ✅ | ✅ | ✅ | sees own |
| Reports (submissions, sales & payouts, competitions) with CSV / print | ✅ | ✅ | ✅ | |
| Announcements, contact inbox | ✅ | ✅ | | reads |
| Audit log | ✅ | | | |
| Public gallery of *Best* paintings, likes | public / ✅ | ✅ | ✅ | ✅ |

👁 = read-only.

### Business rules (kept from the original spec, and enforced server-side)

* One entry per student per competition; JPEG only (content-checked, ≤ 10 MB, ≥ 300×300 px), with a poem / quotation / story / description.
* Editing or withdrawing an entry is only possible **before the deadline**.
* A submission after the deadline is accepted only after an explicit confirmation and is **automatically Disqualified**; it can never be re-graded.
* Grades: Pending → Best / Better / Good / Moderate / Normal / Disqualified. Disqualification requires a reason.
* Awards only for evaluated, non-disqualified paintings in a **concluded** competition; each tier once, one award per painting.
* Only **Best** paintings can be forwarded to an exhibition. Sold paintings keep their records.
* Sales compute the institute's **commission** (per exhibition) and the student's net share; payouts cannot exceed the net share.

## Added on top of the original

* Public **gallery** with search, sort and likes; a public **contact form** feeding an admin inbox
* **In-app notifications** (grades, awards, sales, payouts, new competitions), **announcements** by audience
* **Certificates** and **sale receipts** as print-ready pages
* **Dashboards with charts** per role, **reports** with CSV export, **audit trail** of every change and sign-in
* **Commission & payout tracking**, automatic **status sync** (competitions open on schedule, exhibitions follow their dates)
* Image **thumbnails**, drag-and-drop upload with preview, dark mode, mobile-friendly layout
* Fixes over the old code: no open self-registration as *Admin* (accounts are created by admins only), lockout after 5 failed sign-ins,
  immediate sign-out of deactivated accounts, anti-forgery on every POST, no credentials in source, security headers,
  guarded deletes, one authorization model instead of copy-pasted per-role views.

## Project layout

```
InstituteOfFineArts/
  Domain/            entities & enums (Competition, Submission, Award, ExhibitionListing, …)
  Data/              AppDbContext (+ audit trail), migrations, DbSeeder
  Services/          image processing, notifications, account provisioning, status sync, CSV/paging helpers
  Controllers/       public + role-aware portal controllers
  ViewModels/        form & dashboard models
  Views/             Razor views, layouts (public / portal / print) and shared partials
  wwwroot/           css (Atelier theme), js, self-hosted libraries, uploads
Database/schema.sql  idempotent SQL script generated from the migration
scripts/             smoke-test.ps1 (every page × every role) and flow-test.ps1 (business rules end-to-end)
```

## Tests

With the app running on a **fresh** database:

```powershell
./scripts/smoke-test.ps1    # 138 checks: every page for every role, allowed and forbidden
./scripts/flow-test.ps1     # 45 checks: uploads, late rules, grading, awards, sales/commission, accounts
```

`flow-test.ps1` changes data — drop the `InstituteOfFineArtsV2` database afterwards (the app re-creates and re-seeds it).

## Migrations

```bash
dotnet tool restore
dotnet dotnet-ef migrations add <Name> --project InstituteOfFineArts --output-dir Data/Migrations
dotnet dotnet-ef migrations script --idempotent --project InstituteOfFineArts -o Database/schema.sql
```
