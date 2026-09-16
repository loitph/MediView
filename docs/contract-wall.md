# MediView — the contract wall

> The three contracts every later stage is measured against, written now while nothing exists to
> bias them. Later stages **implement** these; they do not renegotiate them.
>
> Change anything here only on purpose, and add a line to [Changes](#changes) saying why.

---

## 1. URL map

One public origin. The YARP gateway is the only thing exposed; no service host or port appears
anywhere in the web app.

| Prefix | Service | Notes |
| --- | --- | --- |
| `/api/identity/**` | Identity | auth, people, schedules |
| `/api/studies/**` | Studies | booking, the Study aggregate, its lock |
| `/api/imaging/**` | Imaging | DICOM upload and retrieve |
| `/api/reporting/**` | Reporting | reports and medications |
| `/hubs/**` | Studies | SignalR — **needs the WebSocket upgrade enabled on the gateway** |
| `/health` | Gateway | aggregated; fans out to all four services |

**The prefix is stripped.** A `PathRemovePrefix` transform removes `/api/<service>`, so each
service stays unaware it sits behind a gateway and is still directly callable in tests. A service
that handles `GET /studies/{id}` is reached at `GET /api/studies/studies/{id}` through the origin.

### Resource shape per service

Pinned so far. Anything added later must already have a home in this table.

| Service | Path (service-local) | Purpose |
| --- | --- | --- |
| Identity | `POST /auth/register` | patient self-registration |
| Identity | `POST /auth/login`, `POST /auth/refresh` | JWT issuance, rotating refresh token |
| Identity | `POST /admin/doctors` | admin creates a doctor + weekly schedule |
| Identity | `GET /doctors/{id}/availability` | schedules expanded into bookable slots |
| Identity | `GET /internal/people/{id}` | snapshot source at booking time |
| Studies | `POST /appointments` | book — produces the Study |
| Studies | `GET /studies` | worklist, sorted `status, priority, study_date` |
| Studies | `GET /studies/{id}` | detail + status timeline |
| Studies | `POST /studies/{id}/lock`, `POST /studies/{id}/lock/heartbeat` | acquire, renew |
| Studies | `POST /internal/studies/{id}/images-imported` | temporary HTTP call, replaced by Kafka in S4 |
| Imaging | `POST /studies/{studyId}/images` | the admin x-ray import simulation |
| Imaging | `GET /studies/{studyId}/series`, `GET /series/{seriesId}/instances` | QIDO-shaped metadata |
| Imaging | `GET /instances/{sopInstanceUid}` | WADO-shaped retrieve — the pixel bytes |
| Reporting | `POST /reports`, `PUT /reports/{id}` | draft, save draft |
| Reporting | `POST /reports/{id}/finalize` | one transaction, three effects |
| Reporting | `GET /reports/{id}/pdf` | export |
| Reporting | `GET /internal/studies/{id}/lock` | lock-ownership check before a write |

---

## 2. Event map

Services never call each other on the write path. Every fact is written to the publisher's own
`outbox_messages` table **in the same transaction as the state change**, and a background relay
is the only component that talks to Kafka.

| Event | Publisher | Topic | Key | Consumer | Effect |
| --- | --- | --- | --- | --- | --- |
| `ImagesImported` | Imaging | `imaging.images-imported` | `StudyId` | Studies | `Study.MarkImagesImported()` — **status stays `todo`** |
| `ReportFinalized` | Reporting | `reporting.report-finalized` | `StudyId` | Studies | `Complete()` or `RequestReDiagnosis()` by decision, then release the lock |
| `StudyStatusChanged` | Studies | `studies.study-status-changed` | `StudyId` | Web (SignalR) | live status on the patient's page |
| `DoctorRegistered` | Identity | `identity.doctor-registered` | `DoctorId` | *(none)* | deliberately unconsumed — a service may emit a fact nobody asked for |
| *(poison messages)* | any | `mediview.dead-letter` | original | *(inspect by hand)* | after 5 failed attempts, so a bad message never blocks its partition |

**Rules that do not bend:**

- **Keyed by `study_id`.** All events for one Study hash to the same partition, so they are
  consumed in order. 6 partitions per topic.
- **Topic names are stable contracts.** Never rename in place.
- **Additive versioning only.** You version an event by adding a field, never by repurposing an
  existing one. Contracts live in `MediView.Contracts` as immutable records carrying ids and
  primitives — never a domain entity.
- **At-least-once, so consumers are idempotent.** Every handler inserts into
  `studies.inbox_messages` first; a duplicate-key violation means "already processed" — commit
  the offset and return. Offsets commit *after* the handler succeeds, never before.

---

## 3. Status table and its guards

The canonical transitions are in [north-star.md](north-star.md#study-status--the-single-source-of-truth).
This section pins **where each guard is enforced**, so no later task can quietly relax one.

### The two guards that bite

| Guard | Enforced in | Fails as |
| --- | --- | --- |
| **`StartRead` requires images imported** — `todo → in_progress` is refused unless `studies.images_imported_at` is set **and** the caller holds the Redis lock | `Study.StartRead()` on the aggregate, not a handler | `DomainException` → `400`; lock conflict → `409` with the current owner |
| **`AdminOverride` requires a reason** — a break-glass transition with an empty reason is not a transition | `Study.AdminOverride(...)`, plus `reason NOT NULL` on `studies.audit_log` | `DomainException` → `400`; no audit row, no override |

### Where the rest is enforced

- **Transitions live on the aggregate.** `StudyStatusHistory` is an owned child collection
  appended *inside* the transition method. If you ever want an `IStudyStatusHistoryRepository`,
  the boundary is wrong.
- **Doctor transitions require the lock.** Redis is the authority; `studies.study_locks` is the
  durable mirror. If the two disagree, Redis wins.
- **No double booking.** Checked in the handler for a friendly error *and* backed by a unique
  index on `(doctor_id, scheduled_start)`, because two requests can pass the check at once.
  Postgres `23505` maps to the same `409`.
- **Import never changes status.** Only a doctor opening the study under lock does.

---

## Cross-cutting contracts

- **One schema per service; no foreign key crosses a schema.** Cross-service references are
  logical uuids, documented as `logical -> identity.patients.id`.
- **Snapshots, not joins.** `patient_name`, `patient_mrn`, `doctor_name` are copied onto the
  Study at booking time. A later rename must not rewrite history — this is how DICOM itself
  works.
- **Errors are RFC-7807 ProblemDetails**, with the correlation id as an extension member.
- **`X-Correlation-Id`** is read or minted at the edge, echoed on the response, forwarded inward,
  and pushed into the Serilog `LogContext`.
- **Never log PHI.** Ids only — no patient name, DOB, MRN, blood type, report text or medication
  notes. See the logging rule in the [README](../README.md).

---

## Changes

| Date | Change | Why |
| --- | --- | --- |
| 2026-09-08 | Initial wall pinned (S0-T03). | Written before S1 so later stages implement rather than negotiate. |
