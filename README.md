# AcxiomCRM — interview project

A C# ASP.NET Core MVC CRM implementation prepared from the supplied AcxiomCRM assignment. Start here, then read `docs/INTERVIEW_GUIDE.md` and `docs/ACCEPTANCE_CHECKLIST.md`.

**Verification status:** see `docs/VERIFICATION.md`. Do not describe a test as passed unless you have run it successfully. This is a local interview application, not a production deployment.

## 1. What you need on your Windows computer

- .NET 10 SDK (the SDK, not only the runtime): https://dotnet.microsoft.com/download/dotnet/10.0
- Visual Studio with ASP.NET/web development support for .NET 10, or VS Code with C# support.
- Internet for initial NuGet restore. Bootstrap/jQuery/Chart.js assets are bundled locally.
- Python 3 only if you want to run the optional automated HTTP smoke tests.
- No separate SQL Server installation is required. EF Core creates a local SQLite database.

## 2. Run it — PowerShell

Extract this ZIP to a normal folder. Open a terminal **inside the extracted project folder**, where this README appears.

```powershell
cd AcxiomCRM
dotnet --version
dotnet restore
dotnet build
dotnet user-secrets set "Bootstrap:Email" "admin@example.com"
```

Choose your own admin password: at least 10 characters, uppercase, lowercase, number and symbol. To avoid showing it on the screen or typing it into command history:

```powershell
$crmSecurePassword = Read-Host "Choose admin password" -AsSecureString
$crmPassword = [System.Net.NetworkCredential]::new("", $crmSecurePassword).Password
dotnet user-secrets set "Bootstrap:Password" "$crmPassword"
Remove-Variable crmPassword, crmSecurePassword
dotnet run
```

Open **http://localhost:5080**. Sign in with the email/password you set. The database and three roles are created on first run. The app stops with an explanatory error if a first-run admin password is not configured.

After the first successful run you can remove the bootstrap password:

```powershell
dotnet user-secrets remove "Bootstrap:Password"
```

The password is now stored by ASP.NET Core Identity as a framework-managed hash. You only need the bootstrap secret again if you deliberately remove the local database.

## 3. Prepare a convincing demo

1. Sign in as Admin. Under **Users & roles**, create a Manager and two SalesExecutive users.
2. Sign in as the first SalesExecutive in a separate browser profile.
3. Create a customer (unique email and unique Indian mobile number, e.g. starting with 6–9 and 10 digits).
4. Create a lead with an expected value greater than zero. Change New → Contacted → Qualified. Use **Convert lead** on its detail page.
5. Inspect the automatically created customer and opportunity. Opportunity close date defaults to 30 days after conversion.
6. Create a follow-up, then complete it. Create an activity of type Call, Meeting, Email or Task.
7. Edit an opportunity through the pipeline and mark Won. View the dashboard charts and reports.
8. Log in as the second SalesExecutive; show that the first user's records are not visible.
9. As Manager, show the complete business pipeline. As Admin, show users, audit entries and exports.
10. Demonstrate invalid amount, date, phone and probability. Explain why API validation also matters.

Do not use real customer or employer data in the demo.

## 4. Run acceptance smoke checks

Keep the app running. In a **second terminal at the extracted root**:

```powershell
$env:ACXIOM_ADMIN_EMAIL = "admin@example.com"
$crmSecurePassword = Read-Host "Admin password" -AsSecureString
$env:ACXIOM_ADMIN_PASSWORD = [System.Net.NetworkCredential]::new("", $crmSecurePassword).Password
python tests/smoke_test.py
Remove-Item Env:ACXIOM_ADMIN_PASSWORD
Remove-Variable crmSecurePassword
```

For fast business-rule checks, run `dotnet run --project tests/DomainChecks` from the extracted root.

The HTTP script creates test users and CRM data, so run it against a throwaway local database. It checks server validation, role/ownership protection, CSRF, APIs, lead conversion, optimistic concurrency, reports, audit and lockout. There is a 61-second pause before lockout checks to reset the authentication rate-limit window. A test failure prints an assertion; save the terminal output for debugging. Also complete the manual checklist for client validation, responsive layout and Chart.js.

## 5. Project map

```text
AcxiomCRM/
  Program.cs                  Dependency injection, security and middleware
  Models/Entities.cs          Identity user, CRM entities, audit entity
  Data/CrmDb.cs               EF Core mappings, foreign keys and unique indexes
  Data/Seed.cs                Roles and secret-configured first admin
  Services/CrmService.cs      Scope checks, validation and CRM workflows
  Services/Auditor.cs         Audit snapshots and authentication events
  ViewModels/Models.cs        DTOs, validation rules and page models
  Controllers/               MVC controllers and secured REST API
  Views/                     Razor views, shared layout and validation
  wwwroot/                   Custom CSS and JavaScript
  Properties/                Local development launch configuration
docs/                        Design, API, testing and interview notes
tests/smoke_test.py           Standard-library HTTP acceptance checks
```

## 6. Key design choices

- **ASP.NET Core Identity**, not a custom password table. Exactly one role per user.
- **EF Core + SQLite** for a portable interview demo. TPC mapping creates separate Customer, Lead, Opportunity, FollowUp and Activity tables with shared base fields. GUID keys avoid cross-table integer sequence issues.
- **MVC + Razor + Bootstrap** for the UI; **Chart.js** for charts. Scripts/styles are pinned and bundled under wwwroot/vendor with their licenses; no CDN connection is needed while demonstrating.
- **Cookie authentication** is shared by MVC and the REST API. Every unsafe request requires antiforgery protection; API callers obtain a request token after login.
- **DTOs** exclude entity navigation graphs and Identity security fields. One superset DTO is reused across modules to keep the interview project approachable; module-specific rules reject invalid payloads.
- **Service-layer scoping** applies to reads and writes, not just menus.
- **Manager scope** is the entire sales organization (one business team). There is no separate multi-team membership model in this assignment implementation.
- **All dates use UTC**, consistently labeled. A production business-timezone policy would be a separate enhancement.
- **Audit and business changes share SaveChanges**, and conversion uses an explicit transaction.
- **Optimistic concurrency** uses an application-managed version GUID, including deletes and conversion.

## 7. Troubleshooting

| Symptom | What to do |
|---|---|
| `dotnet` is not recognized | Install .NET 10 SDK and reopen the terminal. |
| SDK does not support `net10.0` | Check `dotnet --list-sdks`; install the matching SDK. |
| NuGet restore fails | Check internet/proxy access to nuget.org; rerun `dotnet restore`. |
| First-run admin setup error | Set both bootstrap secrets in the AcxiomCRM directory with a compliant password. |
| Port 5080 is busy | Stop the other app or use `dotnet run --urls http://localhost:5081`; set `ACXIOM_BASE_URL` for tests. |
| Charts or styling missing | Keep the wwwroot/vendor folder; open through dotnet run rather than opening a Razor file directly. |
| API returns 400 | Include the CSRF header and valid DTO fields; see API.md. |
| API returns 401 | Log in and retain the session cookie. |
| API returns 404 for a known record | Check assigned ownership; inaccessible IDs intentionally return 404. |
| Account locked | Wait 15 minutes or have another Admin unlock it. |
| Login returns 429 | Wait for the one-minute authentication rate-limit window. |
| Linked record cannot be deleted/reassigned | The application preserves references and prevents broken ownership relationships. |

## 8. Deployment boundary

This package targets a local interview demonstration. Before real deployment: update and audit dependency patches, replace EnsureCreated with reviewed EF migrations, configure HTTPS and trusted proxy forwarding, disable public registration or add invitations/email confirmation, store deployment secrets in a secret manager, persist Data Protection keys, establish backups/restore tests, restrict database file access and use production monitoring. The in-process IP rate limiter is a local protection; distributed deployments need shared limits. No email/SMS delivery or password-reset email service is implemented. Admin-assisted password reset is implemented.

Technical reference sources used: Microsoft Identity model documentation and antiforgery documentation, plus official NuGet package pages. URLs are in `docs/ARCHITECTURE.md`.
