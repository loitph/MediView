# MediView - the 45-day build, resequenced as a tour

**Same scope. Same 45 days. Same 93 tasks, same task ids.** Only the *order*, the *grouping* and the *gates* changed.

The old plan (P01 - P12) was a **build order**: finish one service, then the next. This is a **tour order**: five passes over the *whole* system, each pass adding one layer of resolution - exactly the way a drawing goes from frame, to pencil outline, to structure, to detail, to colour.

```
S0  Frame       Day 0        look at the finished picture, write the north star
S1  Sketch      Days 1-9     the whole outline in pencil - everything exists, nothing works
S2  Structure   Days 10-20   the domain core + the lock, provable from dotnet test
S3  Detail      Days 21-36   four vertical slices, API and UI together, each one demo-able
S4  Colour      Days 37-45   Kafka, audit, tests, PDF, design pass, demo
```

---

## 1. What was wrong with the old order

Not the content - the content is good. The **sequence** was the problem, and specifically these seven things:

1. **You never saw the whole picture until day 41.** P10 (the UI) is one 14-task block at the very end. The first time anything is visible end to end is day 41 of 45. For a learner that is backwards: you memorised 12 parts and only met the machine in the last week.
2. **Each service was finished to 100% before the next started.** You learned Identity in isolation on days 5-9 and only found out how it connects on day 30. Detail before outline.
3. **The gateway arrived on day 33.** For 30 days you address services on four ports, then change the addressing model once, late. "Single public origin" is a *shape*, and shape belongs in the sketch.
4. **Rework was written into the plan.** The architecture doc says P04-P07 use "temporary direct HTTP calls" that P08 replaces. That is fine as a teaching move - but it is only a teaching move if the swap is a deliberate final stage, not a surprise in the middle.
5. **The star of the project was in the middle.** The Redis single-writer lock is the differentiator and the hardest thing you will build. It sat at P06 (day 22-25), and could not be *proved* until the UI existed on day 41. Risk this big belongs early.
6. **The 15-table picture never existed as one artefact.** You already have a finished DBML, but the old order dribbles it out as four separate first migrations across four weeks. You never once sat and drew the whole schema.
7. **Tests and the demo lived in a 2-day cliff at the end.** P12 has 6 tasks including CI, docs, demo data and "buffer" in two days. That buffer is not real.

---

## 2. The principle behind the new order

> **Every stage covers the entire system. Later stages raise the resolution, they do not add new parts.**

Three rules fall out of that, and they are what actually changed:

- **Breadth before depth.** All four services exist on day 4, empty. All 15 tables exist on day 7, anaemic. All eight pages have a shell before any page has a feature.
- **Risk before comfort.** The state machine and the lock come before booking, before DICOM, before any screen - and are proved with tests, not clicks.
- **Never split a use case from its page.** In S3 a slice is `handler + endpoint + page`. It ends with something you can show someone.

And one gate: **you do not enter a stage until the previous checkpoint is true.** There are six checkpoints, and they are the only hard dependencies in the plan.

---

## 3. The five stages

### S0 - The Frame (Day 0, half a day, 3 tasks)

No code. You already own five artefacts that describe the finished picture - read them, then compress them into one page and three contracts you will not renegotiate later.

**Ends when:** `docs/north-star.md` and `docs/contracts.md` exist, and you can recite the four statuses and their guards without looking.

---

### S1 - SKETCH: the walking skeleton (Days 1-9, 30 tasks)

Pencil. Everything exists; nothing has an opinion. Four services that start, four schemas holding all 15 tables, the YARP gateway already being the single origin, a Blazor shell that already renders, a JWT that already validates at the edge.

**You deliberately write no business rule here.** `StartRead`, `Finalize` and `AdminOverride` do not exist yet. The entities have public setters and that is correct for now.

**Ends when (`S1-T99`):** sign in as a seeded doctor through the gateway, render a worklist page from a real `studies` row, and follow that one request across services in Seq by correlation id.

---

### S2 - STRUCTURE: the domain core and the lock (Days 10-20, 17 tasks)

The stage that teaches DDD and the stage that carries the risk - so it comes early, and it comes **without a UI**. Aggregates, owned children, invariants, the status state machine, and the Redis single-writer lock.

The lock moved forward roughly ten days. It is the differentiator, it is the hardest thing in the project, and discovering it is hard on day 14 is survivable in a way that discovering it on day 25 is not.

**Ends when (`S2-T99`):** `dotnet test` shows illegal transitions throwing, `StartRead` without images throwing, `AdminOverride` without a reason throwing, a finalized report refusing edits, and two doctors racing for one Study with exactly one winner and a 409 carrying the owner. No browser involved.

---

### S3 - DETAIL: four vertical slices (Days 21-36, 34 tasks)

Now the picture gets worked up, in the order of the golden path, so the build and the tour are the same walk. Each slice is backend *and* frontend, and each slice ends in a demo.

| Slice | Days | The demo at the end |
|---|---|---|
| A - identity in a user's hands | 21-24 | register a patient, admin onboards Dr. B, slots appear |
| B - book, snapshot, worklist | 25-28 | a booking becomes a Study that shows as **To do** with Open disabled |
| C - images and the viewport | 29-32 | admin imports the .dcm set, status stays **To do**, Cornerstone draws the frame |
| D - the read itself | 33-36 | the golden path, end to end, two browsers, lock and all |

**Ends when (`S3-T93`):** `register -> book -> import -> read -> finalize -> done` runs in a browser without you touching the database by hand - **on day 36, with nine days left.** That is the single biggest change in this plan: the old order reached this point on day 41 with four.

---

### S4 - COLOUR (Days 37-45, 21 tasks)

Ink and colour over a finished drawing. Everything here improves something that already works.

- **Swap the transport** - Kafka + outbox replaces the temporary direct calls. Not one boundary moves. That is the microservice lesson, and it only lands because you felt the synchronous version first.
- **The healthcare lesson** - audit, mandatory-reason override, force-release, the break-glass page.
- **Trust** - authorization tests, security pass, Testcontainers, e2e, CI.
- **Finish** - PDF, design pass against the mockups, ADR, README, demo script, buffer.

**Ends when (`S4-T99`):** you can give the ten-minute walkthrough in the order you built it - frame, sketch, structure, detail, colour - without opening an IDE.

---

## 4. What moved, and why

| Task | Was | Now | Why |
|---|---|---|---|
| `P09-T01..T04` gateway | Day 33 | S1, day ~6 | Single origin is a shape, not a feature. Fix the addressing model before 30 days of code assumes ports. |
| `P10-T01` Blazor shell | Day 34 | S1 | You need a canvas from the start, even an empty one. |
| `P03-T05/T06` JWT + policies | Days 7-8 | S1 | Auth is skeleton plumbing. Registration (`P03-T04`) is the feature and stays in S3. |
| `P05-T01`, `P07-T01`, `P04-T01` scaffolds | Spread over days 10-26 | S1, all in one sitting | They are the same exercise four times. Doing them together teaches the template; doing them a fortnight apart teaches nothing. |
| `P06-*` the Redis lock | Days 22-25 | S2, days ~15-20 | Highest risk, highest teaching value, and provable by test with no UI. |
| `P04-T12`, `P07-T09`, `P06-T08` unit tests | Late / P12 | S2, beside the code they test | A rule that is only checked by careful clicking is not a rule. |
| `P10-T02..T13` the eight pages | One block, days 34-41 | Split across the four S3 slices | A use case and its page are one unit of work. |
| `P04-T11` admin override | Day 15 | S4 | It is break-glass, and break-glass belongs with audit. |
| `P07-T07` PDF export | Day 29 | S4 | Pure polish, and a classic time sink mid-project. |
| `P01-T07` pipeline behaviours | Day 3 | S2 | Validation behaviours need handlers to wrap. In S1 there are none. |

**Nothing was deleted.** All 93 original tasks are present with their original ids, details, steps and acceptance criteria. 12 new tasks were added: 3 orientation, 1 schema sketch, 1 reshaping migration, 6 checkpoints/demos, 1 demo day.

---

## 5. Two honest tensions

**"Sketching the schema first is not DDD."** Correct - a purist models behaviour and lets the schema fall out. Two reasons to do it your way here: the DBML is already finished, so you are transcribing a decision rather than making one; and drawing the whole outline is the specific thing your old plan never let you do. The mitigation is built in: S1 entities are anaemic *on purpose* and `S2-T90` is a migration that **reshapes the sketch to fit the domain**. Pencil, then ink. If S2 makes you change a table, the process worked.

**S1 is 30 tasks in 9 days.** It looks heavy. They are almost all scaffolding, four near-identical repetitions, and copy-from-the-DBML work - low thinking, high typing. If you slip, slip S1 into day 10-11 and take it out of the S3 slice C budget (DICOM is where the plan has the most slack, because `P05-T08` sample data and `P10-T06` interop can be timeboxed).

---

## 6. The full sequence

Stage / group / task, in order. `(new)` marks a task that did not exist in `mvp-v1`.

### S0 - The Frame (Day 0, 3 tasks)

**Orient**
- `S0-T01` Read the five reference artefacts in one sitting *(new)*
- `S0-T02` Write the one-page north star *(new)*
- `S0-T03` Pin the contract wall - URL map, event map, status table *(new)*

### S1 - SKETCH: the walking skeleton (Days 1-9, 30 tasks)

**Tooling**
- `P01-T01` Pin the .NET 10 SDK with global.json
- `P01-T02` Create the solution skeleton and folder layout
- `P01-T03` Central package management and shared build properties
- `P01-T04` Add .editorconfig and analyzers

**Runtime**
- `P02-T01` docker-compose.yml with the four dependencies
- `P02-T02` Connection strings and user-secrets
- `P02-T03` Serilog to console and Seq
- `P02-T04` Run scripts and health checks

**Shared kernel**
- `P01-T05` BuildingBlocks.Domain: Entity, AggregateRoot, domain events
- `P01-T06` BuildingBlocks.Application: Result<T> and a hand-rolled dispatcher
- `P01-T08` Error handling middleware + correlation id
- `P01-T09` Contracts project for integration events

**Four shells**
- `P03-T01` Scaffold the four Identity projects and the DbContext
- `P04-T01` Scaffold the Studies service from the Identity template
- `P05-T01` Scaffold the Imaging service and model Series/Instance
- `P07-T01` Scaffold Reporting and model the Report aggregate

**The schema sketch**
- `S1-T90` Sketch all 15 tables in one pass, straight from the DBML *(new)*
- `P03-T02` Model User, Patient, Doctor and DoctorSchedule
- `P03-T03` EF configurations, schema and first migration
- `P04-T05` EF configurations, snake_case mapping and optimistic concurrency
- `P07-T03` EF configuration, drug seed and migration

**The edges**
- `P09-T01` YARP project: one origin, path-based routing to four services
- `P09-T02` JWT validation at the edge
- `P09-T03` CORS, rate limiting and correlation-id propagation
- `P09-T04` Aggregated health and API docs
- `P03-T05` Login, JWT issuance and refresh-token rotation
- `P03-T06` Three authorization policies
- `P03-T09` Endpoints, seed data and a .http smoke file
- `P10-T01` App shell, auth state and typed API clients

**Checkpoint**
- `S1-T99` CHECKPOINT - the skeleton walks *(new)*

### S2 - STRUCTURE: the domain core and the lock (Days 10-20, 17 tasks)

**The Study aggregate**
- `P04-T02` Study aggregate root and its status state machine
- `P04-T03` StudyStatusHistory as an owned child collection
- `P04-T04` Appointment entity and the no-double-booking invariant
- `P04-T06` Repositories and the unit-of-work rule
- `P04-T12` Unit tests for the state machine

**The Report aggregate**
- `P07-T02` Finalize invariants - the rules that make a report clinical
- `P07-T09` Unit tests for the Report aggregate

**Rails**
- `P01-T07` Validation and logging pipeline behaviours
- `S2-T90` Migration #2 - reshape the sketch to fit the domain *(new)*

**The single-writer lock**
- `P06-T01` Define the IStudyLockService port
- `P06-T02` Redis lock adapter: SET NX PX with an owner token
- `P06-T03` Acquire on open: the todo -> in_progress transition
- `P06-T04` Heartbeat and TTL renewal
- `P06-T05` Release paths and the expiry sweeper
- `P06-T06` StudyLockedException -> 409 with the owner in the body
- `P06-T08` The concurrency test that proves it

**Checkpoint**
- `S2-T99` CHECKPOINT - the rules are green without a browser *(new)*

### S3 - DETAIL: four vertical slices (Days 21-36, 34 tasks)

**A - identity in a user's hands**
- `P03-T04` Patient self-registration
- `P03-T07` Admin creates a doctor and their weekly schedule
- `P03-T08` Availability query - expand schedules into bookable slots
- `P10-T02` Page 1 - sign in and register
- `P10-T11` Page 6 - admin doctor onboarding
- `S3-T90` DEMO - a patient exists and a doctor has hours *(new)*

**B - book, snapshot, worklist**
- `P04-T07` BookAppointment: the snapshot pattern in action
- `P04-T08` The doctor worklist query
- `P04-T09` Study detail and patient timeline queries
- `P10-T03` Page 2 - patient books a checkup
- `P10-T05` Page 4 - doctor worklist
- `P10-T04` Page 3 - patient studies, status timeline and result
- `S3-T91` DEMO - a booking becomes a Study on the worklist *(new)*

**C - images and the viewport**
- `P05-T02` IBlobStore port with a local-disk adapter
- `P05-T03` Parse uploaded DICOM with fo-dicom
- `P05-T04` Upload endpoint - the admin x-ray simulation
- `P05-T05` Publish ImagesImported
- `P05-T06` QIDO-shaped metadata endpoints
- `P05-T07` WADO-shaped retrieve endpoint
- `P05-T08` Sample data set and an imaging smoke test
- `P04-T10` MarkImagesImported
- `P10-T12` Page 7 - admin x-ray import simulation
- `P10-T06` Cornerstone.js interop module
- `P10-T07` Page 5 - the DICOM viewport
- `S3-T92` DEMO - images arrive and the viewport draws them *(new)*

**D - the read itself**
- `P06-T07` SignalR StudyHub
- `P10-T08` Lock banner, heartbeat and the read-only state
- `P07-T04` Create draft and save draft - lock ownership enforced
- `P07-T05` Medication composer commands
- `P07-T06` Finalize: one transaction, three effects
- `P07-T08` Report queries for both portals
- `P10-T09` Report editor and medication composer panel
- `P10-T10` SignalR client as a single scoped service
- `S3-T93` DEMO - the golden path, end to end, in a browser *(new)*

### S4 - COLOUR (Days 37-45, 21 tasks)

**Swap the transport**
- `P08-T01` Kafka client, shared producer, topics and partitioning
- `P08-T02` Transactional outbox + a Kafka relay
- `P08-T03` Publish the integration events through the outbox
- `P08-T04` Kafka consumers in the Studies service
- `P08-T05` Idempotent consumers, a dead-letter topic and an end-to-end test

**The healthcare lesson**
- `P04-T11` Admin status override with a mandatory reason
- `P11-T01` Audit writes as a first-class part of the command
- `P11-T02` Force-release lock, end to end
- `P10-T13` Page 8 - admin audit log and break-glass

**Trust**
- `P11-T03` Prove the authorization boundary with tests
- `P11-T04` The four security checks worth doing now
- `P12-T01` One integration test per service with Testcontainers
- `P12-T02` The end-to-end happy path test
- `P12-T03` GitHub Actions build and test workflow

**Finish**
- `P07-T07` PDF export with QuestPDF
- `P10-T14` Design pass against the mockups
- `P11-T05` Write the deferred-scope ADR
- `P12-T04` README, architecture doc and diagrams
- `P12-T05` Demo script and seeded demo data
- `P12-T06` Buffer and honest scope review

**Checkpoint**
- `S4-T99` DEMO DAY - tell the tour back *(new)*

---

## 7. Your files, and what each one is for now

| File | Role in the tour | Changed? |
|---|---|---|
| `mediview-overview.html` | S0 reading, step 1 - the product | no |
| `study-lifecycle.png` | S0 reading, step 2 - the rules | no |
| `mediview-architecture.html` | S0 reading, step 3 - the shape | no |
| `mediview-mvp.dbml` | S0 reading, step 4; then the source for `S1-T90`; updated by `S2-T90` | no (you will edit it in S2) |
| `MediView_UI_-_MVP_dc.html` | S0 reading, step 5; then the spec for each S3 slice and for `P10-T14` | no |
| `checklist-tree-mvp.html` | superseded | keep as the v1 archive |
| **`checklist-tree-mvp-v2.html`** | **the guide you work from daily** | **new** |

The four reference artefacts were already good - they *are* the whole picture, which is why S0 exists. The only thing that needed rebuilding was the checklist, because the checklist was the thing claiming to be a tour and behaving like a build queue.

`checklist-tree-mvp-v2.html` is the same interactive tool, same tree, same step-ticking, same progress export - re-driven by the new five-stage sequence. Task ids are unchanged, so any progress already saved in the browser lines up. Dependencies were rewritten to match the new order, so the "ready to start" queue in the sidebar is now a genuine *next action* list: at any moment it shows the handful of tasks the tour has actually unlocked.
