# Verification record

Verified on 14 October 2026 in a Linux test environment using .NET SDK 10.0.402 / runtime 10.0.13. Project dependencies target .NET 10 and pin the EF/Identity packages at 10.0.12.

## Passed

- Application build: zero compiler warnings and zero errors, including Razor compilation.
- Fresh SQLite schema initialization, Identity roles and first administrator creation.
- 19 domain validation / lead-transition checks (`DOMAIN_TEST_RESULTS.txt`).
- 58 HTTP smoke assertions (`TEST_RESULTS.txt`) against a running local application with a fresh database.
- HTTP checks cover authentication, authorization, ownership, server validation, duplicate checks, CSRF, workflow conversion, stale updates/deletes, CRM module pages, reports, CSV, audit, activity CRUD, opportunity outcomes, deactivation and lockout.
- Main MVC pages returned HTTP 200 under valid authorization; forbidden pages returned the expected status.
- Python smoke-test script syntax checked.
- Custom JavaScript syntax checked with Node.

## Not verified here

- Browser-rendered desktop/mobile layout, actual Chart.js rendering, keyboard behavior and client-side validation interactions. Playwright was available, but its Chromium download returned invalid archives; no browser assertions ran.
- Windows-specific setup. The instructions use standard .NET SDK commands, but execution was tested on Linux.
- Production deployment, load testing, backup recovery, email or external integrations.

Complete the manual browser acceptance checklist before presenting. Do not infer visual browser verification from successful server-rendered page requests.

No real customer records, passwords, local test databases, SDK binaries, or build output are included in the package. Temporary test credentials were generated solely for local verification.
