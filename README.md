# MediView
```
Booking, Diagnosis, DICOM viewport
```

## Getting started

### Prerequisites

- .NET SDK 10.0.100 or later (pinned in `global.json`)
- Docker with Compose v2.20 or later (`docker compose up --wait`)
- Bash, for `run.sh` (Git Bash or WSL on Windows)

### Configure

Ports and the Postgres login live in `.env`. `run.sh` copies `.env.example` to `.env` on the
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
