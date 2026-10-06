# MediView

A small radiology workflow, built to learn one idea well: **one doctor edits a study at a time**.
A patient books a checkup inside a doctor's hours, an admin imports the x-rays, the doctor reads
them in a DICOM viewport under a Redis lock and writes a report, and the study status closes the
loop. Everyone else sees the study as LOCKED.

**Register → book → import → read under lock → finalize → done.**

### Architecture in five lines

1. Four .NET 10 services, each with Domain / Application / Infrastructure / Api layers: **Identity** (people, schedules, JWT), **Studies** (booking, the study state machine, the lock), **Imaging** (DICOM upload and bytes), **Reporting** (reports and medications).
2. A YARP **gateway** is the only thing the Blazor Server **web app** calls; the web app forwards `/api/**` to it so the browser has one origin.
3. Each service owns one Postgres database; services call each other over plain HTTP with the caller's JWT, never through each other's tables.
4. The single-writer lock lives only in **Redis** (`SET NX PX` plus Lua compare-and-act); a second doctor gets `409` naming the owner.
5. The viewport is **Cornerstone.js**, vendored into the web app; DICOM files are stored on local disk.

The frame behind every decision is [docs/north-star.md](docs/north-star.md); the service contracts
are in [docs/contract-wall.md](docs/contract-wall.md).

## Getting started

### Prerequisites

- .NET SDK 10.0.100 or later (pinned in `global.json`)
- Docker with Compose v2.20 or later (`docker compose up --wait`)
- Bash and `openssl`, for `run.sh` and the signing key (Git Bash or WSL on Windows)
- A Chromium-based browser or Firefox; the viewport needs WebGL

Node is not needed: the viewer libraries are already built and committed.

### Configure

Run every command from the repository root. Ports and the Postgres login live in `.env`. `run.sh` copies `.env.example` to `.env` on the
first run; edit it before then if a port is already taken.

Connection strings are kept out of `appsettings.json` and set per service with user-secrets.
Use the password from `.env`:

```bash
for service in Identity Studies Imaging Reporting; do
  lower=$(echo "$service" | tr '[:upper:]' '[:lower:]')
  dotnet user-secrets set "ConnectionStrings:Postgres" \
    "Host=localhost;Port=15432;Database=mediview_$lower;Username=mediview;Password=mediview" \
    --project "src/Services/$service/MediView.$service.Api"
done
```

The four services validate the same JWT, so they share one signing key of at least 32 bytes.
A service refuses to start without it:

```bash
key=$(openssl rand -base64 48)
for service in Identity Studies Imaging Reporting; do
  dotnet user-secrets set "Jwt:SigningKey" "$key" --project "src/Services/$service/MediView.$service.Api"
done
```

### Run

```bash
./run.sh
```

It starts Postgres, Redis and Seq with `docker compose up -d --wait`, builds the solution once,
then runs the four services, the gateway and the web app. Ctrl+C stops the .NET processes and
the containers (`docker compose stop`, so data in the volumes is kept). To run only the
infrastructure, use `docker compose up -d` and `docker compose stop`.

| What | URL |
| --- | --- |
| Web | http://localhost:5217 |
| Gateway | http://localhost:5062 |
| Seq | http://localhost:5341 |
| Identity / Studies / Imaging / Reporting | http://localhost:5156 / 5134 / 5169 / 5145 |

Every service answers `GET /health` with `200 Healthy`.

### Seeded accounts

In Development, Identity seeds three users into an empty database. All share the password
`MediView#2026`. The doctor works Monday to Friday, 08:00-16:00, in 30-minute slots.

| Role | Email |
| --- | --- |
| Admin | admin@mediview.local |
| Doctor | doctor@mediview.local |
| Patient | patient@mediview.local |

`src/Services/Identity/MediView.Identity.Api/MediView.Identity.Api.http` logs in as each one,
directly and through the gateway.

### Sample x-rays

`samples/chest-phantom/` holds a 12-slice synthetic CT of a chest phantom (`Patient A`,
uncompressed DICOM, no real patient data). The [Demo](#demo) imports it in step 3.

Uploaded files are stored under `.data/imaging/` (git-ignored); the root is `BlobStorage:Root` in
the Imaging service's `appsettings.json`.

## Demo

Fifteen minutes from `./run.sh` to a finalized report. Open http://localhost:5217. Each browser
tab keeps its own sign-in, so "a second browser" can be a second tab; sign out from the sidebar
between roles in the same tab.

1. **Patient books.** Sign in as `patient@mediview.local` (or register a new patient). Open
   **Book a checkup**, pick *Dan Doctor*, pick a free slot and **Confirm booking**. The slot
   disappears from the grid, and **My studies** shows the study as *To do*.
2. **Admin adds a second doctor.** Sign out, sign in as `admin@mediview.local`. On **Doctors**,
   create `Dr. B` (`dr.b@mediview.local`, password `MediView#2026`, any licence and specialty).
   You need them for the lock moment.
3. **Admin imports the x-rays.** Open **Import x-rays**, **Select** the patient's study, drop the
   twelve files from `samples/chest-phantom/` and **Import x-rays**. The study shows *Imported*
   and is still *To do*.
4. **Doctor reads under the lock.** In a new tab, sign in as `doctor@mediview.local`. The
   worklist shows the study; **Open** it. The status moves to *In progress* and the lock pill
   says *You*. Scroll with the wheel, drag to change window/level, try Pan, Zoom and Reset.
5. **The two-browser lock moment.** Copy the viewport address. In another tab, sign in as
   `dr.b@mediview.local` and paste it. Dr. B gets a red **READ-ONLY** banner naming Dan Doctor,
   the same images, and no editor.
6. **Write and finalize.** Back as Dan Doctor, type findings and an impression (each saves when
   you leave the field), add a medication, keep **Complete** and click **Finalize & release
   lock**. You land on the worklist with the study *Done*, and the lock is free.
7. **Patient sees the result.** As the patient, refresh **My studies**: *Done*, the timeline, and
   the finalized report with its medications.

Two optional extras, two minutes each:

- **Re-diagnosis.** Finalize with **Re-diagnosis** instead. The study moves to *Re-diagnosis*;
  open it again and a fresh draft starts, with the first report kept underneath. Reports are never
  edited after finalizing, only followed by a new one.
- **Break-glass.** As the admin, open **Import x-rays** and click the study number. The study page
  lists every status change with who made it. Type a reason, then **Override status** or **Force
  release lock**; both refuse an empty reason, and the reason lands in the history.

## Development

### Cornerstone.js

The viewer libraries are vendored into `src/Web/MediView.Web/wwwroot/lib/cornerstone` (no CDN).
To upgrade them, change the versions in `tools/cornerstone/package.json` and rebuild:

```bash
cd tools/cornerstone && npm install && npm run build
```

### Database migrations

In Development each service applies its pending EF Core migrations on startup, so `./run.sh` keeps
every database current. To change an entity and add a migration, see
[docs/migrations.md](docs/migrations.md).

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| On the very first start each service logs one `ERR ... An error occurred using the connection to database` | Expected: EF Core probes a database that has no migration history yet, then migrates. `Application started` follows. |
| A service exits at start with `Connection string 'Postgres' is not configured` | Run the first user-secrets loop in [Configure](#configure). |
| A service exits with `Jwt:SigningKey must be at least 32 bytes` | Run the signing-key loop; all four services need the same key. |
| `docker compose` says a port is already allocated | Change `POSTGRES_PORT`, `REDIS_PORT` or `SEQ_PORT` in `.env` and use the new Postgres port in the connection strings. |
| `address already in use` for 5156, 5134, 5169, 5145, 5062 or 5217 | Another copy of the app is still running; stop it (Ctrl+C in its `run.sh`). |
| Booking shows no free slots | The seeded doctor works Monday to Friday, 08:00-16:00; use **Later** to move to the next days. |
| A study stays LOCKED after its doctor closed the tab | Closing a tab does not release the lock; it expires 15 minutes after the last heartbeat. An admin can **Force release lock** with a reason. |
| You want a clean slate | `docker compose down -v` and `rm -rf .data`, then `./run.sh` reseeds everything. |

## Known limits

Deliberate, so the build stays inside its 45 days; the full list is "Deliberately out of scope" in
[docs/north-star.md](docs/north-star.md), and the review of what is rough is in
[docs/retrospective.md](docs/retrospective.md).

- Desktop only; the viewport needs WebGL and has been tried with uncompressed DICOM only.
- Only the assigned doctor can start a read. After a forced lock release the assigned doctor
  opens the study again; another doctor still sees it read-only.
- One instance of each service; no email, no live updates (pages read status when they load).

## Logging rule (non-negotiable)

Log identifiers, never protected health information.

- OK: `StudyId`, `PatientId`, `DoctorId`, `UserId`, `CorrelationId`, status transitions, timings
- NO: patient name, date of birth, MRN, blood type, report text, medication notes, DICOM pixel data

Every service logs through `builder.AddApiDefaults()` (Serilog to console + Seq) and
`app.UseApiDefaults()` (correlation id, then one summary line per request). Each line
carries `ServiceName`, `CorrelationId` and — once the caller is authenticated — `UserId`.

Seq runs at http://localhost:5341 (`docker compose up -d seq`). To follow one request end to end,
filter on `CorrelationId = '...'`; callers can pin the id themselves by sending an
`X-Correlation-Id` header, and it is echoed back on every response.
