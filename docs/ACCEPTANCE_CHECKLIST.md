# Manual acceptance checklist

Record actual results and screenshots on your computer. The existence of this checklist is not evidence that the checks passed.

## Setup and authentication

- [ ] `dotnet restore` and `dotnet build` succeed.
- [ ] Fresh first-run database and bootstrap Admin are created successfully.
- [ ] Protected MVC pages redirect to Login; anonymous API calls return 401.
- [ ] Register creates a SalesExecutive and redirects to Dashboard.
- [ ] Valid login succeeds; wrong credentials fail without exposing account existence.
- [ ] Five wrong passwords cause lockout; admin unlock works.
- [ ] Logout invalidates the session.
- [ ] Deactivated user cannot log in or reuse an existing session.
- [ ] Admin password reset works; no old password hash appears in HTML/API/audit.

## Core workflow and validation

- [ ] Create/edit/details/delete for each CRM module (or relationship conflict explained).
- [ ] Customer name required and maximum length enforced.
- [ ] Email and phone are validated in browser and on crafted HTTP requests.
- [ ] Duplicate customer email and phone are rejected.
- [ ] Lead expected value has a valid range; unknown statuses rejected.
- [ ] New → Contacted → Qualified → Convert creates one customer and one opportunity.
- [ ] Double conversion rejected; Converted cannot be set by ordinary editing.
- [ ] Opportunity amount 0/negative, probability >100 and active past close date rejected.
- [ ] New follow-up in past rejected, including a forged Completed status.
- [ ] Follow-up completion/reschedule creates an audit entry.
- [ ] Activity Call/Meeting/Email/Task can be recorded and searched.
- [ ] Stale edit/delete returns a conflict rather than overwriting data.
- [ ] Invalid or unauthorized foreign keys are rejected.

## Authorization and UI

- [ ] SalesExecutive A cannot read/change/delete SalesExecutive B's record by URL/API.
- [ ] SalesExecutive cannot reassign ownership to another user.
- [ ] Manager can manage business-wide CRM and reports, but cannot administer users or audit security.
- [ ] Admin can create users, assign exactly one role, activate/deactivate, reset and unlock.
- [ ] All record names and notes are HTML-encoded (test `<script>` text displays harmlessly).
- [ ] Desktop/tablet/phone layout remains usable.
- [ ] Required field errors are understandable and visible; keyboard navigation works.
- [ ] Delete and convert actions show confirmation dialogs.
- [ ] List search, sorting, filters and pagination preserve the selected filters.

## Analytics, APIs and history

- [ ] Dashboard cards match database scope and selected date period.
- [ ] Chart.js lead, pipeline and monthly won-sales charts render.
- [ ] Won/Lost are excluded from open pipeline; weighted amount calculated correctly.
- [ ] Upcoming/overdue list is accurate relative to UTC date.
- [ ] Reports and CSV have the same authorized scope.
- [ ] Conversion rate handles zero leads without division by zero.
- [ ] API returns 201 on create, 200 on read/update and 204 on delete.
- [ ] Invalid/unauthorized/conflicting requests return 400/401/403/404/409 as documented.
- [ ] Missing CSRF token prevents unsafe requests.
- [ ] Audit contains login success/failure, logout, create/update/delete, conversion, role/reset/unlock events.
- [ ] No UI/API route can edit or delete an audit row.
- [ ] Data persists after stopping and restarting the app.

## Before the interview

- [ ] Run `tests/smoke_test.py` successfully on a local test database.
- [ ] Remove accidental personal/customer data from the demo.
- [ ] Prepare three browser profiles (Admin, Manager, SalesExecutive).
- [ ] Practice the 10-minute walkthrough in INTERVIEW_GUIDE.md.
- [ ] Explain one request end to end without reading a script.
- [ ] Be honest about assistance and known limitations.
