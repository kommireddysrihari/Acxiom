# REST API

The API uses the same ASP.NET Core Identity session cookie as the MVC application. It does not expose public anonymous CRM endpoints or a JWT issuance endpoint.

## Authentication and antiforgery

1. Sign in at `/Account/Login`, retaining the authentication cookie.
2. GET `/api/csrf`, retaining the antiforgery cookie. The response contains `requestToken`.
3. For POST, PUT and DELETE, add `X-CSRF-TOKEN: <requestToken>` and send the cookies.
4. For JSON bodies use `Content-Type: application/json`.
5. Log out using the MVC logout form; then protected API calls return 401.

A CSRF token is a required request-verification token, not an access token or password.

## Routes

For `{kind}`, use `customers`, `leads`, `opportunities`, `followups` or `activities`.

| Method | Route | Result |
|---|---|---|
| GET | `/api/{kind}` | Paged, scoped list: `total`, `page`, `pageSize`, `items`. |
| GET | `/api/{kind}/{id}` | Scoped record DTO. |
| POST | `/api/{kind}` | Create; 201 plus Location header. |
| PUT | `/api/{kind}/{id}` | Update; include current `version`; 200. |
| DELETE | `/api/{kind}/{id}?version={version}` | Delete; 204; references may cause 409. |
| POST | `/api/leads/{id}/convert?version={version}` | Convert qualified lead; 200 customer DTO. |
| GET | `/api/reports/pipeline` | Stage/owner groups with amount, weighted amount and count. |
| GET | `/api/csrf` | Authenticated antiforgery request token. |

List parameters: `q`, `status`, `owner`, `from`, `to`, `sort`, `page`, `pageSize` (1–100). Sort: newest, oldest, name, amount. Dates are `YYYY-MM-DD`. For follow-ups and activities, list date filters use the activity date; other list modules use creation date.

## Customer request

Replace the owner ID with an existing active user ID from the assignment dropdown. Sales users can assign only themselves.

```json
{
  "kind": "customers",
  "name": "Demo Customer",
  "email": "demo@example.com",
  "phone": "9876543210",
  "companyName": "Example Company",
  "address": "Main Road",
  "city": "Tuni",
  "state": "Andhra Pradesh",
  "status": "Active",
  "assignedTo": "OWNER-IDENTITY-ID",
  "notes": "Interview sample"
}
```

Email is normalized to lowercase, phone follows the documented 10-digit Indian mobile rule, and both must be unique among customers. For updates, send the latest returned `version` along with the fields. IDs and creation metadata are server-controlled.

## Lead request

Use name, email, phone, companyName, assignedTo, notes, and additionally:

```json
{"kind":"leads","status":"New","source":"Website","priority":"Normal","expectedValue":50000}
```

Allowed progression: New → Contacted → Qualified → Convert. New or Contacted can move to Lost; Contacted can become Unqualified; Unqualified may return to Contacted or become Lost. Converted and Lost are terminal. Conversion is a separate operation so it cannot be simulated by changing the status field.

## Opportunity request

Use name, assignedTo, notes, source and:

```json
{"kind":"opportunities","customerId":"CUSTOMER-GUID","leadId":null,"amount":100000,"probability":50,"stage":"Proposal","status":"Open","expectedCloseDate":"2030-12-01"}
```

Supply your actual future close date. Stage is Qualification, Proposal, Negotiation, Won or Lost. Status must be Open for the first three, or match Won/Lost. Amount must be positive; probability 0–100. Expected close date cannot be past for Open records. `ClosedDate` is set by the service when won/lost.

## Follow-up / activity requests

Follow-up: name (subject), assignedTo, customerId/leadId/opportunityId (at least one), followUpDate, followUpType (Call/Meeting/Email/Task), status (Planned/Completed/Missed/Cancelled), notes (remarks).

Activity: name (subject), assignedTo, customerId/leadId (at least one), activityDate, activityType (Call/Meeting/Email/Task), status (Planned/Completed/Cancelled), notes (description).

New follow-ups cannot be created in the past even if the caller submits a completed status. Existing planned activities cannot be rescheduled to the past.

## Browser-console example

After logging in, open developer tools on the application page:

```javascript
const token = await fetch('/api/csrf').then(r => r.json());
const list = await fetch('/api/customers').then(r => r.json());
console.table(list.items);
// Obtain a real assignedTo ID from a customer or the creation form before POSTing.
```

## Status and error behavior

- 200: read/update/conversion success.
- 201: created.
- 204: deletion complete.
- 400: malformed JSON, validation failure or missing/invalid antiforgery token.
- 401: no valid session.
- 403: role/assignment forbidden.
- 404: missing or out-of-scope record.
- 409: duplicate, stale version or referential conflict.
- 429: authentication rate limit.

Business errors use `{ "error": "message" }`. Framework model-binding errors use ASP.NET ValidationProblemDetails; antiforgery errors can be an empty/framework 400 response. Clients should check status before trying to parse JSON. This documented difference is a deliberate limitation, not a promise of one universal error envelope.
