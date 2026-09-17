# MediView — the contract wall

> The three contracts every later stage is measured against. Later stages **implement** these;
> they do not renegotiate them.
>
> Change anything here only on purpose, and add a line to [Changes](#changes) saying why.

---

## 1. URL map

One public origin. The YARP gateway is the only thing the web app calls; no service host or port
appears anywhere in the web app.

| Prefix | Service | Notes |
| --- | --- | --- |
| `/api/identity/**` | Identity | auth, people, schedules |
| `/api/studies/**` | Studies | booking, the Study aggregate, its lock |
| `/api/imaging/**` | Imaging | DICOM upload and retrieve |
| `/api/reporting/**` | Reporting | reports and medications |

**The prefix is stripped.** A `PathRemovePrefix` transform removes `/api/<service>`, so each
service is unaware it sits behind a gateway and stays directly callable. A service that handles
`GET /studies/{id}` is reached at `GET /api/studies/studies/{id}` through the origin. Every
service exposes `GET /health`.

### Resource shape per service

Anything added later must already have a home in this table.

| Service | Path (service-local) | Purpose |
| --- | --- | --- |
| Identity | `POST /auth/register` | patient self-registration |
| Identity | `POST /auth/login` | JWT issuance (8h, no refresh token) |
| Identity | `POST /admin/doctors`, `GET /doctors` | admin creates a doctor + weekly schedule; public list |
| Identity | `GET /doctors/{id}/availability` | schedules expanded into slots |
| Identity | `GET /internal/people/{userId}`, `GET /internal/doctors/{id}` | snapshot source at booking time |
| Studies | `POST /appointments` | book — produces the Study |
| Studies | `GET /studies`, `GET /studies/{id}`, `GET /studies/booked-slots` | worklist / my studies, detail + timeline, taken slots |
| Studies | `POST /studies/{id}/open` | acquire the lock and start the read |
| Studies | `GET /studies/{id}/lock`, `POST /studies/{id}/lock/heartbeat`, `POST /studies/{id}/lock/release` | owner, renew, release |
| Studies | `POST /studies/{id}/override`, `POST /studies/{id}/lock/force-release` | admin break-glass, reason required |
| Studies | `POST /internal/studies/{id}/images-imported` | called by Imaging |
| Studies | `POST /internal/studies/{id}/finish-read` | called by Reporting on finalize |
| Imaging | `POST /studies/{studyId}/images` | the admin x-ray import |
| Imaging | `GET /studies/{studyId}/instances`, `GET /instances/{id}/file` | list, and the original bytes |
| Reporting | `POST /reports`, `PUT /reports/{id}`, `GET /reports?studyId=` | draft, save, read |
| Reporting | `POST /reports/{id}/medications`, `DELETE /reports/{id}/medications/{medId}` | medication rows |
| Reporting | `POST /reports/{id}/finalize` | freeze the report, then finish the read in Studies |

---

## 2. Service-to-service calls

There is no message broker. A service calls another through a typed `HttpClient` that forwards
the caller's bearer token, so the callee authorizes the real user.

| Caller | Callee | When | Effect |
| --- | --- | --- | --- |
| Studies | Identity `GET /internal/people/{id}`, `/internal/doctors/{id}` | booking | copy `patient_name`, `patient_mrn`, `doctor_name` onto the Study |
| Imaging | Studies `POST /internal/studies/{id}/images-imported` | after a successful upload | `Study.MarkImagesImported()` — **status stays `todo`** |
| Reporting | Studies `GET /studies/{id}/lock` | before every report write | not the owner → `409` |
| Reporting | Studies `POST /internal/studies/{id}/finish-read` | after finalize commits | `Complete()` or `RequestReDiagnosis()` by decision, then release the lock |

**Rules:**

- **The callee is the authority.** A caller never writes another service's data or schema.
- **A failed call after a commit is retried, not rolled back.** A finalized report stays
  finalized; the page offers to retry `finish-read`.
- Every transition in `finish-read` and `images-imported` is **idempotent**: calling it twice
  leaves the study where the first call put it.

---

## 3. Status table and its guards

The canonical transitions are in [north-star.md](north-star.md#study-status--the-single-source-of-truth).
This section pins **where each guard is enforced**, so no later task can quietly relax one.

### The two guards that bite

| Guard | Enforced in | Fails as |
| --- | --- | --- |
| **`StartRead` requires images imported** — `todo → in_progress` is refused unless `images_imported_at` is set **and** the caller holds the Redis lock | `Study.StartRead()` on the aggregate, lock taken first in the handler | `DomainException` → `400`; lock held by someone else → `409` with the owner's name |
| **`AdminOverride` requires a reason** — a break-glass transition with an empty reason is not a transition | `Study.AdminOverride(...)` | `DomainException` → `400`; no history row, no override |

### Where the rest is enforced

- **Transitions live on the aggregate.** `StudyStatusHistory` is an owned child collection
  appended *inside* the transition method. It is also the audit trail: actor, reason, from → to.
- **Doctor transitions require the lock.** Redis is the only copy of it.
- **No double booking.** Checked in the handler for a friendly error *and* backed by a unique
  index on `studies.studies (doctor_id, scheduled_start)`, because two requests can pass the
  check at once. Postgres `23505` maps to the same `409`.
- **Import never changes status.** Only a doctor opening the study under lock does.

---

## Cross-cutting contracts

- **One schema per service; no foreign key crosses a schema.** Cross-service references are
  logical uuids.
- **Snapshots, not joins.** `patient_name`, `patient_mrn`, `doctor_name` are copied onto the
  Study at booking time. A later rename must not rewrite history — this is how DICOM itself
  works.
- **Errors are RFC-7807 ProblemDetails.** `DomainException` → 400, `ConflictException` → 409.
- **`X-Correlation-Id`** is read or minted by each service, echoed on the response and pushed into
  the Serilog `LogContext`.
- **Never log PHI.** Ids only — no patient name, DOB, MRN, blood type, report text or medication
  notes. See the logging rule in the [README](../README.md).

---

## Changes

| Date | Change | Why |
| --- | --- | --- |
| 2026-09-08 | Initial wall pinned (S0-T03). | Written before S1 so later stages implement rather than negotiate. |
| 2026-09-17 | Kafka event map replaced by four HTTP calls; SignalR, refresh tokens, lock mirror, audit table and PDF removed. | Plan cut to 54 tasks: keep the golden path and the lock, drop transport and polish. |
