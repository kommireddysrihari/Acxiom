# Interview walkthrough — understand before you present

This is a study guide. Present only features you have personally run and can explain. If asked about assistance, describe it honestly and focus on the code you understand and have verified.

## Your 60-second introduction

“AcxiomCRM is a role-based CRM built with ASP.NET Core MVC. It manages customers, leads, opportunities, follow-ups and activities. ASP.NET Core Identity handles login, password hashing, roles and lockout. Entity Framework Core persists the data in SQLite. I placed business rules and ownership checks in a shared service so both MVC screens and REST endpoints follow the same rules. The dashboard and reports are generated from authorized data, and important changes are recorded in an audit log.”

## A 10-minute demonstration

| Time | Demonstrate | Explain |
|---|---|---|
| 0–1 min | Login and dashboard | Identity, role scope and actual database data. |
| 1–2 min | Customer creation | Required fields, email/mobile formats, unique database indexes. |
| 2–4 min | Lead → Contacted → Qualified → Convert | Explicit transitions; atomic customer/opportunity creation. |
| 4–5 min | Opportunity validation | Positive amount, 0–100 probability and future active close date. |
| 5–6 min | Follow-up and activity | Related entities, owner checks, dates, completion. |
| 6–7 min | Switch to second sales account | Scope enforced on direct URL/API access too. |
| 7–8 min | Manager reports / CSV | Scoped analytics, stage totals and conversion denominator. |
| 8–9 min | Admin users and audit | Exactly one role, activation, reset, lockout, security stamp. |
| 9–10 min | API and test results | Appropriate status codes, DTOs, CSRF and server validation. |

## Questions you should be ready to answer

**What is MVC?** Model represents application data; View renders the interface; Controller handles requests. Here controllers delegate CRM business work to CrmService rather than containing all validation and persistence logic.

**Authentication versus authorization?** Authentication identifies the signed-in user. Authorization decides which operations and records that user may access. A valid SalesExecutive cookie does not grant access to someone else's customer.

**Why Identity?** It already provides password hashing, password policies, roles, lockout, cookies and reset token generation. Reimplementing passwords is unnecessary and risky. ApplicationUser only adds activation state.

**Are passwords encrypted?** They are hashed by Identity, not reversibly encrypted. Authentication verifies a candidate password against the stored hash. Never claim your own hashing algorithm was implemented.

**Why both client and server validation?** Client validation gives quick feedback. Attackers or API clients can bypass it. The server repeats rules before database writes. A unique index also protects against two simultaneous duplicate submissions.

**Where is ownership enforced?** CrmService.Query restricts every SalesExecutive query by AssignedTo. Find uses that scoped query, including on updates and deletes. Reference validation checks linked records and compatible owners. Menus alone are not a security boundary.

**What is a DTO?** A data transfer object defines the request/response fields. It avoids exposing EF navigation graphs or Identity password hashes and lets the service ignore client-supplied server-managed fields. This implementation uses a superset RecordDto for simplicity; separate DTOs per module would be cleaner at larger scale.

**What prevents overposting?** Apply copies only DTO fields that exist on the target record and excludes IDs, versions, creation/outcome timestamps and conversion metadata. Administrative endpoints use a separate view model and require Admin.

**How does conversion work?** A qualified lead is loaded in the caller's scope and checked for a matching version. The service checks duplicates and expected value, creates a customer and opportunity, marks the lead converted, adds audit entries, and commits in one transaction. A failure rolls everything back.

**What is optimistic concurrency?** Each record has a version GUID. An edit sends the version it read. A mismatch returns 409 instead of silently overwriting someone else's change. EF's concurrency token also protects the final database update.

**What is CSRF?** An attacker can try to make a browser send its authentication cookie to perform an unwanted action. Forms include antiforgery tokens; API mutations require a matching X-CSRF-TOKEN header. GET requests do not change business data.

**Why does the API use cookies rather than JWT?** It serves the same-origin MVC application, so the existing Identity session is sufficient. External clients could use a separate standards-based token authentication integration; that is not implemented here.

**How do you prevent SQL injection and XSS?** EF LINQ queries parameterize values; there is no concatenated raw SQL. Razor encodes record values. The only raw output is server-serialized chart JSON, which uses the default safe JSON encoder.

**What is weighted pipeline?** Amount × Probability / 100. A ₹100,000 opportunity at 40% contributes ₹40,000 weighted pipeline. The dashboard's open pipeline excludes Won and Lost.

**What does “monthly sales” mean here?** Won opportunity amounts grouped by actual ClosedDate month. The dashboard's creation-date filter first selects the cohort of opportunities. This distinction is displayed so expected dates are not confused with realized outcomes.

**How is audit protected?** There are no audit edit/delete endpoints; access is restricted, and CrmDb rejects tracked AuditLog changes/deletes. It is append-oriented at application level, not cryptographically tamper-proof against a database administrator.

**What are the limitations?** Local SQLite, UTC business dates, one manager-wide business team, in-memory report filtering suited to a small demo, bundled frontend assets, no email notifications or automated reminder delivery, no production migrations yet. Mentioning real tradeoffs is better than claiming production readiness.

## Learn the code in this order

1. Models/Entities.cs — identify tables, foreign keys and shared record fields.
2. ViewModels/Models.cs — read each annotation and Validate rule.
3. Data/CrmDb.cs — explain IdentityDbContext, unique indexes and TPC mapping.
4. Program.cs — dependency injection and middleware order.
5. AccountController — Identity sign-in and audit events.
6. CrmService — Query, Validate, Save and Convert.
7. CrmController and one Razor form — trace one request from browser to database.
8. ApiController — trace the same service via JSON.
9. ReportsController and dashboard — calculate metrics by hand on a few sample records.
10. tests/smoke_test.py — know what each security assertion proves and what it does not.

## A useful practice exercise

Create two opportunities: ₹100,000 at 50% and ₹60,000 at 25%. Before closing either, your open pipeline should be ₹160,000 and weighted pipeline ₹65,000. Mark the first Won. Open pipeline becomes ₹60,000, weighted open pipeline ₹15,000, and current-month won sales becomes ₹100,000. Show these calculations during your demo.
