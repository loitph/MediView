# MediView — Engineering Standards

Decisions, not options. Each section says **what to do** and **why**, so every service
looks the same and reviews are about the domain, not the plumbing.

---

## 1. Clean Architecture + DDD — the four layers

Each service is four projects. Dependencies point **inward only**.

| Project | Layer | Contains | May reference |
|---|---|---|---|
| `MediView.<Svc>.Domain` | Enterprise rules | Entities, aggregates, value objects, domain events, **repository interfaces**, domain exceptions | *nothing* |
| `MediView.<Svc>.Application` | Application rules | Use cases (commands/queries + handlers), DTOs, validators, **port interfaces** (`IEmailSender`, `IStudyLock`) | Domain |
| `MediView.<Svc>.Infrastructure` | Interface adapters | EF Core `DbContext`, configurations, repository implementations, Redis, message bus, blob, gRPC clients | Application, Domain |
| `MediView.<Svc>.Api` | Frameworks & drivers | Minimal API endpoints, middleware, DI composition root, `Program.cs` | Infrastructure, Application, Domain |

**The rule that makes it real:** `Domain.csproj` has **zero** package references beyond the
BCL. If you ever need `Microsoft.EntityFrameworkCore` in Domain, the design is wrong — move
the type to Infrastructure and put an interface in Domain.

**Aggregate boundaries** (one transaction = one aggregate):
`Study` (root; status, history, lock), `Report` (root; addenda), `Prescription` (root; items),
`Bill` (root; items, payments), `Wallet` (root; transactions), `Appointment`, `Doctor` (credentials).

---

## 2. Folder & file structure

### Solution layout

```
MediView/
├── MediView.sln
├── docker-compose.yml
├── Directory.Packages.props          # central package versions
├── .editorconfig  .gitignore
├── docs/                             # architecture.md, adr/
├── build/                            # Dockerfiles, CI scripts
├── src/
│   ├── BuildingBlocks/
│   │   ├── MediView.BuildingBlocks.Domain/        # Entity, AggregateRoot, ValueObject, IDomainEvent
│   │   ├── MediView.BuildingBlocks.Application/   # ICommand, IQuery, Result<T>, behaviours
│   │   ├── MediView.BuildingBlocks.Infrastructure/# EF base, outbox, Redis, auth extensions
│   │   └── MediView.Contracts/                    # integration events + gRPC .proto (shared)
│   ├── Services/
│   │   ├── Auth/          MediView.Auth.{Domain,Application,Infrastructure,Api}
│   │   ├── Identity/      MediView.Identity.{...}
│   │   ├── Patient/       MediView.Patient.{...}
│   │   ├── Booking/       MediView.Booking.{...}
│   │   ├── Imaging/       MediView.Imaging.{...}
│   │   ├── Reporting/     MediView.Reporting.{...}
│   │   ├── Prescription/  MediView.Prescription.{...}
│   │   ├── Billing/       MediView.Billing.{...}
│   │   └── Notification/  MediView.Notification.{...}
│   ├── Gateway/  MediView.Gateway            # YARP
│   └── Web/      MediView.Web                # Blazor
```

### Inside one service (Patient shown)

```
MediView.Patient.Domain/
├── Studies/                      # folder per aggregate, NOT per pattern
│   ├── Study.cs                  # AggregateRoot
│   ├── StudyStatus.cs            # enum + transition rules
│   ├── StudyStatusHistory.cs
│   ├── Events/StudyDiagnosisStarted.cs
│   └── IStudyRepository.cs       # interface lives WITH its aggregate
├── Patients/  Patient.cs  Mrn.cs  IPatientRepository.cs
└── Common/    DomainException.cs

MediView.Patient.Application/
├── Studies/
│   ├── Commands/StartDiagnosis/  {Command, Handler, Validator}.cs
│   ├── Queries/GetWorklist/      {Query, Handler, Response}.cs
│   └── Ports/IStudyLockService.cs
└── DependencyInjection.cs        # AddApplication()

MediView.Patient.Infrastructure/
├── Persistence/
│   ├── PatientDbContext.cs
│   ├── Configurations/StudyConfiguration.cs
│   ├── Repositories/StudyRepository.cs
│   ├── Queries/StudyReadQueries.cs      # Dapper read models
│   └── Migrations/
├── Locking/RedisStudyLockService.cs
├── Messaging/StudyStatusChangedPublisher.cs
└── DependencyInjection.cs        # AddInfrastructure(config)

MediView.Patient.Api/
├── Endpoints/StudyEndpoints.cs   # Minimal API, one file per aggregate
├── Hubs/StudyHub.cs              # SignalR
├── Grpc/PatientGrpcService.cs
├── Middleware/ExceptionHandlingMiddleware.cs
├── Program.cs
└── appsettings.json
```

**Rule:** folders are named after **domain concepts** (`Studies/`), never after patterns
(`Repositories/`, `Services/`). You should be able to see what the system does from `ls`.

---

## 3. EF Core — the write side

```csharp
public class StudyConfiguration : IEntityTypeConfiguration<Study>
{
    public void Configure(EntityTypeBuilder<Study> b)
    {
        b.ToTable("studies", "patient");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.StudyInstanceUid).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.StudyInstanceUid).IsUnique();
        b.OwnsOne(x => x.Mrn);                       // value object
        b.Property(x => x.Version).IsRowVersion();   // xmin
        b.HasMany(x => x.History).WithOne().OnDelete(DeleteBehavior.Cascade);
    }
}
```

Standards:
- **One DbContext per service**, mapped to that service's schema (`HasDefaultSchema("patient")`).
- **Never** a `DbSet` for another service's tables.
- `EnableRetryOnFailure()` on Npgsql; `UseSnakeCaseNamingConvention()`.
- Concurrency via Postgres `xmin` — a lost update throws `DbUpdateConcurrencyException`, which the
  middleware maps to **409**.
- Lazy loading **off**. Load explicitly with `Include` or project.
- Migrations live in Infrastructure: `dotnet ef migrations add X -p ...Infrastructure -s ...Api`.

**Unit of Work = `SaveChangesAsync`.** Don't wrap `DbContext` in a custom `IUnitOfWork` that
just forwards; the repository interface plus `SaveChangesAsync` is the pattern.

---

## 4. LINQ vs Dapper — when to use which

| Use | For | Why |
|---|---|---|
| **EF Core + LINQ** | Commands: load aggregate → mutate → save | Change tracking and invariants are the point |
| **EF Core + LINQ + `AsNoTracking()` + `Select`** | Simple reads | No tracking overhead; projection avoids `SELECT *` |
| **Dapper** | Worklist, dashboards, audit search, reports over joins | Hand-tuned SQL, flat DTO, no expression-tree translation |

Always project — `Select` into a DTO, never return entities from a query handler:

```csharp
// read model: Dapper, one round trip
const string Sql = """
    SELECT s.id, s.study_number, s.modality, s.diagnosis_status,
           p.mrn, l.doctor_id AS lock_owner_id
    FROM patient.studies s
    JOIN patient.patients p ON p.id = s.patient_id
    LEFT JOIN patient.study_locks l ON l.study_id = s.id AND l.released_at IS NULL
    WHERE s.diagnosis_status = ANY(@Statuses)
    ORDER BY s.priority DESC, s.study_date
    LIMIT @Take OFFSET @Skip;
    """;
```

**Never** paginate in memory (`.ToList().Skip()`), and never `foreach` a query issuing more
queries — that's the N+1 that kills the worklist.

### Data structures — pick deliberately
- `Dictionary<TKey,T>` for lookups in a loop (O(1)); building one beats repeated `.FirstOrDefault()` (O(n²)).
- `HashSet<T>` for "have I seen this?" and set math (permission codes).
- `IReadOnlyList<T>` / `IReadOnlyCollection<T>` on public APIs — expose intent, not `List<T>`.
- `record` for DTOs and value objects (structural equality, immutability).
- `IAsyncEnumerable<T>` for streaming large DICOM instance lists.
- Inside an aggregate, expose `IReadOnlyCollection<T>` over a private `List<T>` so invariants hold.

---

## 5. Data access — joins across a service boundary

You cannot `JOIN` across services. Three legal options:

1. **Compose in the gateway/UI** — two calls, join in the client. Fine for detail screens.
2. **gRPC call** — synchronous, when you need one field now (`GetDoctorName(doctorId)`).
3. **Local replica via events (preferred for lists)** — Patient keeps a tiny `doctor_directory`
   read table (`doctor_id`, `full_name`, `department`) updated from `DoctorVerified` events.
   The worklist then joins **locally** and stays fast.

Rule of thumb: **queries on the hot path replicate; commands call.**

---

## 6. API, middleware, JWT

Pipeline order in `Program.cs` (order matters):

```
UseExceptionHandler → UseSerilogRequestLogging → UseCors
→ UseAuthentication → UseAuthorization → UseRateLimiter → MapEndpoints
```

- **Custom middleware:** `CorrelationIdMiddleware` (accepts/creates `X-Correlation-Id`, pushes to
  the log scope), `ExceptionHandlingMiddleware` (maps `DomainException`→400, `NotFound`→404,
  `StudyLockedException`→**409 with the owner in the body**, `DbUpdateConcurrencyException`→409),
  `AuditMiddleware` (writes `audit.access_log` for PHI reads).
- **JWT:** access token ~15 min, RS256 so the gateway and every service validate with the public
  key; refresh tokens rotate, single-use, hashed at rest. Validate issuer, audience, lifetime,
  signing key, `ClockSkew = 30s`.
- **Authorization is permission-based, not role-based:**
  `.RequireAuthorization("study.diagnose")` — because Admin has almost every permission but
  **not** `study.diagnose`. Roles map to permissions; policies check permissions.
- REST conventions: plural nouns, `PATCH /studies/{id}/status`, `ProblemDetails` for errors,
  `Idempotency-Key` header on payment POSTs, cursor pagination on lists.

---

## 7. SignalR — real-time, and where sockets fit

The viewport needs live state, so **SignalR** (WebSockets with fallback) is the transport. Use it for:

| Hub | Event | Consumer |
|---|---|---|
| `StudyHub` | `LockAcquired` / `LockReleased` / `LockExpiring` | Doctors viewing the study |
| `StudyHub` | `StatusChanged` | Worklist, patient status page |
| `WorklistHub` | `StudyImported` | Doctor dashboard |
| `NotificationHub` | `NotificationCreated` | Bell / toast |

Standards:
- **Groups, not connection ids:** `Groups.AddToGroupAsync(conn, $"study:{studyId}")`.
- **Backplane required** once you scale past one instance: Redis backplane
  (`AddStackExchangeRedis`) — otherwise a doctor on instance B never sees instance A's release.
- **Authenticate the hub** (`[Authorize]`); JWT arrives via `access_token` query string for WS —
  wire `OnMessageReceived`.
- **The hub is not the source of truth.** Redis holds the lock; the hub only *announces*. A
  dropped socket must never grant or extend a lock.
- **Lock heartbeat:** client pings every 30s → `last_heartbeat`; TTL 15 min idle → auto-release,
  publish `LockReleased`, and notify everyone in `patient.lock_watchers` ("Notify me when free").
- Don't use raw WebSockets — you'd rebuild groups, reconnection and backplane by hand. Raw sockets
  only if you later stream pixel data outside HTTP, which WADO-RS already covers.

---

## 8. Service communication — gRPC vs message broker

**Rule: commands that need an answer → gRPC. Facts that already happened → events.**

| Style | Use for | Example |
|---|---|---|
| **gRPC** (sync, typed, fast) | Query another service inside a request | Booking → Identity `GetDoctorAvailability` |
| **RabbitMQ / MassTransit** (async) | Broadcast a fact; anything that must not block | `ReportFinalized`, `StudyStatusChanged`, `BillPaid` |

- Contracts (`.proto` + event records) live in `MediView.Contracts`, versioned, never edited in place —
  add a field, don't repurpose one.
- **Transactional outbox** (MassTransit EF outbox): the domain change and the event publish commit
  in the **same transaction**, then a relay publishes. Without it, a crash between save and publish
  silently loses the event.
- **Consumers must be idempotent** — dedupe on `MessageId`; at-least-once delivery is guaranteed,
  exactly-once is not.
- Retry with exponential backoff + jitter, then a **dead-letter queue** you actually monitor.
- Choreography example on finalize: Reporting saves the report → publishes `ReportFinalized` →
  Patient sets status, Prescription issues the RX, Billing raises the diagnosis-fee bill,
  Notification emails the patient. No service calls another synchronously.

---

## 9. DI, configuration, logging

**DI** — each layer owns its registration; `Program.cs` reads as a table of contents:

```csharp
builder.Services
    .AddApplication()                       // handlers, validators, behaviours
    .AddInfrastructure(builder.Configuration) // DbContext, repos, Redis, bus
    .AddJwtAuth(builder.Configuration)
    .AddPermissionPolicies();
```

Lifetimes: `DbContext`/repositories/handlers **scoped**; stateless helpers and
`IConnectionMultiplexer` **singleton**; **never** inject a scoped service into a singleton
(use `IServiceScopeFactory` in the background worker).

**Configuration** — bind to typed options and validate on start, so a bad env var fails at boot,
not at 2 a.m.:

```csharp
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations().ValidateOnStart();
```

Precedence: `appsettings.json` → `appsettings.{Env}.json` → env vars → Key Vault (prod). **No
secrets in the repo**; user-secrets locally.

**Logging** — Serilog, JSON to console (containers log to stdout), enriched with
`CorrelationId`, `UserId`, `ServiceName`. Structured, never interpolated:

```csharp
_logger.LogInformation("Study {StudyId} moved {From}->{To} by {UserId}", id, from, to, userId);
```

**Never log PHI** — no patient name, DOB, or report text. Log ids. This is the single easiest
compliance mistake to make. Health checks at `/health/live` and `/health/ready`; OpenTelemetry
traces so one request is followable across gateway → service → bus.

---

## 10. Blazor

- **Blazor Server** for the doctor's viewport (low latency, DICOM stays server-side, no big WASM
  download). If you want offline/CDN hosting later, WASM is the swap — keep components free of
  server-only types so the choice stays open.
- Structure: `Features/Studies/`, `Features/Booking/` — same domain-first folders as the services.
- **Typed HTTP clients** per service (`IStudyApi`) registered via `AddHttpClient` with Polly
  retry/circuit-breaker. No `HttpClient` news-up in a component.
- `CascadingAuthenticationState` + `<AuthorizeView Policy="study.diagnose">` so the UI hides what
  the API would refuse anyway — **UI checks are convenience; the API is the authority.**
- Cornerstone.js via **JS interop with an `IJSObjectReference` module**, disposed in
  `IAsyncDisposable`. Never leak listeners between studies.
- SignalR client in a scoped service, not per-component, so one connection serves the shell.
- Render: `@key` on lists, `ShouldRender` on the viewport, virtualization on the worklist.

---

## 11. Docker & cloud

- **Multi-stage Dockerfile**, non-root user, `--no-restore` layering so restore caches:
  SDK build stage → `dotnet publish` → `mcr.microsoft.com/dotnet/aspnet:10.0` runtime.
- `docker-compose.yml` for local: postgres, redis, rabbitmq, seq/otel, gateway, services, web.
  One `docker compose up` must give a working system — that's the bar.
- **.NET Aspire** for local orchestration + service discovery if you want the developer experience;
  it does not replace compose in CI.
- Azure: images → **ACR**; services → **Container Apps** (scale rules, revisions); Postgres
  Flexible Server, Azure Cache for Redis, Service Bus (swap for RabbitMQ in prod), Blob for DICOM,
  Key Vault + **managed identity** (no connection strings in config).
- Health probes wired to Container Apps; private endpoints for data services; budget alerts on.

---

## 12. Cross-cutting standards

- **Result over exceptions** for expected failures (`Result<T>` with an error code); exceptions only
  for the genuinely exceptional. Middleware maps both to `ProblemDetails`.
- **Pipeline behaviours** for validation, logging, and transactions so handlers stay pure domain logic.
- **Time is injected** (`IClock`) — never `DateTime.Now` (and always `DateTimeOffset.UtcNow`;
  store `timestamptz`, render in the user's zone).
- **Money is `decimal`**, never `double`, and always with an explicit currency.
- **Human-readable numbers** (`MRN-1042`, `INV-0031`, `RX-0114`) come from a sequence generator, not
  `COUNT(*) + 1` — that race duplicates under load.
- **Soft-delete clinical records.** Nothing in a patient's history is hard-deleted; a finalized
  report is corrected by an **addendum**, never edited.
