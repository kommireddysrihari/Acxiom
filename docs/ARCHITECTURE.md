# Architecture and requirement mapping

## Request flow

Browser → MVC controller → CrmService → EF Core DbContext → SQLite.
JSON client → API controller → the same CrmService → the same database.
Identity, authorization, validation, auditing and antiforgery span both flows.

## Database

ASP.NET Core Identity supplies AspNetUsers, AspNetRoles, AspNetUserRoles and its other framework tables. ApplicationUser extends IdentityUser with IsActive. There is no separate custom password table.

CrmRecord is an abstract base mapped with table-per-concrete-type (TPC). Every concrete CRM table includes Id, Name, Status, AssignedTo, Notes, CreatedDate, ModifiedDate, CreatedBy and Version. Guid IDs are generated in application code. AssignedTo references an Identity user.

| Table | Specific fields / relationships |
|---|---|
| Customers | CustomerCode, Email, Phone, CompanyName, Address, City, State. Unique email, phone, code indexes. |
| Leads | LeadCode, Email, Phone, CompanyName, Source, Priority, ExpectedValue, ConvertedCustomerId, ConvertedDate. |
| Opportunities | CustomerId, optional LeadId, Amount, Stage, Probability, ExpectedCloseDate, ClosedDate, Source. |
| FollowUps | CustomerId/LeadId/OpportunityId, FollowUpDate, FollowUpType. |
| Activities | CustomerId/LeadId, ActivityDate, ActivityType. |
| AuditLogs | Id, UserId, Action, EntityName, RecordId, OldValue, NewValue, CreatedDate, IpAddress, Result. |

Naming aliases versus the PDF: entity `Id` is CustomerId/LeadId/etc.; `Name` is CustomerName/LeadName/OpportunityName or activity subject; `Notes` is remarks/description. These are naming differences, not omitted data. Generic UI forms reuse those base labels.

## Security decisions

- Identity hashes and validates passwords, with 10-character complexity policy.
- Lockout: five failed attempts → 15 minutes.
- Auth route rate limit: 20 requests/minute/IP. Includes GETs, login, registration and logout.
- Cookie HttpOnly, SameSite Lax, 30-minute sliding expiry; secure-only outside Development.
- Security stamp validated on each request; administrative security changes invalidate old sessions.
- Activation checked on every authenticated request.
- MVC unsafe requests use globally registered AutoValidateAntiforgeryTokenAttribute, which is the global equivalent of adding ValidateAntiForgeryToken to each POST action.
- Same-origin cookie-authenticated API also requires antiforgery on mutations.
- Admin and Manager have business-wide CRM scope; SalesExecutive has assigned scope.
- Reference checks prevent linking a record to an inaccessible object or assigning an inconsistent sales owner.
- Linked record reassignment is rejected instead of silently breaking ownership; coordinated multi-record reassignment is future work.
- EF parameterizes database input; Razor encodes display content.
- Audit snapshots use DTOs, never Identity user objects or password fields.
- CSV export prefixes formula-like cells to prevent spreadsheet formula execution.

## Requirement traceability

| Assignment area | Implementation |
|---|---|
| Application shell and role navigation | Views/Shared/_Layout.cshtml, wwwroot/css/site.css |
| Login/register/logout/lockout | AccountController, Identity configuration in Program.cs |
| Admin/Manager/SalesExecutive | Roles, authorization attributes, CrmService.Query |
| Customer CRUD and uniqueness | CrmService.Save/Delete, CrmDb unique indexes |
| Lead workflow and conversion | CanTransition, Convert transaction |
| Opportunity rules and weighted pipeline | RecordDto.Validate, CrmService, dashboard/reports |
| Follow-ups and activities | Entities, shared controller/forms, date rules |
| User administration / reset / activation | AdminController |
| Dashboard cards and Chart.js | DashboardController, Views/Dashboard/Index.cshtml |
| Client validation | Razor data annotations, unobtrusive scripts, native input attributes, site.js |
| Server/business validation | DTO annotations/Validate plus CrmService.Validate |
| Foreign key and ownership validation | CrmService.Validate and database constraints |
| REST API | Controllers/ApiController.cs |
| Audit security/business changes | Auditor, Admin/Audit, record history |
| Reports, filters, CSV export | ReportsController and Views/Reports |
| Acceptance verification | tests/smoke_test.py plus manual checklist |

## Tradeoffs and improvements

1. The small demo loads scoped records before report filtering. A production system should push filtering, pagination and grouping into SQL and add measured indexes.
2. EnsureCreated is convenient for a first-run local database. Use migrations for schema evolution; it does not update an existing database to new entity definitions.
3. A single RecordDto reduces repetitive code but exposes some irrelevant default fields in responses. Separate input/output DTOs per module improve contract clarity.
4. Managers have whole-business visibility; no organizational hierarchy is modeled.
5. Audit is application-level append-only, not an external immutable ledger.
6. Follow-up reminders are visible upcoming/overdue lists, not email/SMS/push notifications.
7. Public registration is optional configuration; default is enabled for interview acceptance. It always assigns SalesExecutive. Production needs invitation/verification policy.
8. There is no public password recovery email flow. Admin-assisted password reset is present.
9. Amounts are decimal. SQLite has limitations for server-side decimal aggregation; the demo computes scoped aggregates in .NET.
10. GDPR retention, distributed job queues, large-scale search and deployment operations are outside this local project.

## Primary references

- Identity model: https://learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0
- Antiforgery: https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
- EF SQLite package: https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite/10.0.12
- Identity EF package: https://www.nuget.org/packages/Microsoft.AspNetCore.Identity.EntityFrameworkCore/10.0.12

Dependencies are pinned for reproducibility; check supported security patches before any real deployment.

Frontend assets are vendored with their original licenses in wwwroot/vendor (Bootstrap 5.3.3, jQuery 3.7.1, jquery-validation 1.21.0, unobtrusive validation 4.0.0, Chart.js 4.4.7).
