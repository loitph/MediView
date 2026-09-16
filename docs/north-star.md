# MediView — north star

> The page that stays open for 45 days. If a later task contradicts it, one of the two is wrong —
> stop and decide which. Re-read it at every stage checkpoint.

## The golden path

**Register → book → import → read under lock → finalize → done.**

A patient self-registers and books inside a doctor's published schedule; an admin simulates the
x-ray import; the assigned doctor opens the study in a Cornerstone.js viewport under a
single-writer lock, writes a report with medications, and finalizes it. The Study status closes
the loop.

## The one rule this project exists to teach

**One doctor edits a Study at a time. Redis is the authority. Everyone else sees LOCKED.**

- The lock is `SET study-lock:{id} {ownerToken} NX PX {ttl}`. Release is a Lua
  compare-and-delete, so you can never release someone else's lock after your own TTL expired.
- `studies.study_locks` in Postgres is a **mirror** — it feeds the UI banner and the audit
  trail. If Redis and Postgres ever disagree, **Redis wins**.
- A second doctor gets `409` carrying the current owner's name, with no extra service call,
  because the owner name is snapshotted onto the lock row.

Everything else in this build is scaffolding around that one idea.

## Study status — the single source of truth

Copied from `static-graph/study-lifecycle.png`. Where each guard is enforced is in
[contract-wall.md](contract-wall.md) §3.

| From | To | Trigger (who) | Guard (must hold) |
| --- | --- | --- | --- |
| *(start)* | **To do** | System | A booking is placed within the doctor's working hours. |
| To do | **In progress** | Doctor (assigned) | Images are imported into the Study **and** the doctor holds the single-writer viewport lock. |
| In progress | **Done** | Doctor (lock owner) | The report is finalized and the decision is "no further read needed". |
| In progress | **Re-diagnosis** | Doctor (lock owner) | The doctor decides another read is needed. |
| Re-diagnosis | **In progress** | Doctor | A doctor re-acquires the viewport lock and resumes the read. |
| any | any | Admin (break-glass) | Only when the doctor is unavailable. Every override is audited — actor, reason, before → after. |

Import does **not** change the status. A study stays *To do* until a doctor opens it under lock.

## Deliberately out of scope

Drifting into any of these costs days you do not have.

- **Billing** — cart, checkout, wallet, payments. An e-commerce sub-domain; teaches neither
  imaging nor DDD.
- **Notifications** — email and SMS. SignalR toasts cover the demo.
- **A permission matrix** — roles, permissions, role_permissions. One `role` column on the user
  plus three policies.
- **Azure and IaC** — no Bicep, no deployment. It runs on `docker compose`, full stop.
- **Separate Auth, Booking and Prescription services** — merged into Identity, Studies and
  Reporting respectively.
- **gRPC, Dapper, OpenTelemetry, .NET Aspire, a Redis SignalR backplane** — each is a real
  technique, and each is a different project.

## The shape

4 services — Identity, Studies, Imaging, Reporting — behind one YARP gateway, with a Blazor
Server web app of 8 pages. PostgreSQL, one schema per service, 15 domain tables; **no foreign
key ever crosses a schema**. Services never call each other on the write path: they coordinate
by choreography over Kafka, each writing to its own transactional outbox in the same
transaction as the state change.

45 days · 105 tasks · S0 → S4. Progress: `python3 plan/check.py`.
