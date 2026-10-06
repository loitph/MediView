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

- The lock is `SET study-lock:{id} "{doctorId}|{doctorName}" NX PX {ttl}`. Renew and release are
  Lua compare-and-act scripts, so you can never release someone else's lock after your own TTL
  expired.
- Redis is the **only** copy of the lock. There is no Postgres mirror and no sweeper: a lock ends
  on release, on finalize, on admin force-release, or when its TTL runs out.
- A second doctor gets `409` carrying the current owner's name, read straight from the lock value.

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
| any | any | Admin (break-glass) | Only when the doctor is unavailable. Every override writes a history row with actor, reason, from → to. |

Import does **not** change the status. A study stays *To do* until a doctor opens it under lock.

## Deliberately out of scope

Drifting into any of these costs days you do not have.

- **Billing** — cart, checkout, wallet, payments. An e-commerce sub-domain; teaches neither
  imaging nor DDD.
- **Notifications and live push** — email, SMS and SignalR. A page asks when it loads; a 409 names
  the lock owner.
- **A permission matrix** — roles, permissions, role_permissions. One `role` column on the user
  plus three policies.
- **Azure and IaC** — no Bicep, no deployment. It runs on `docker compose`, full stop.
- **Separate Auth, Booking and Prescription services** — merged into Identity, Studies and
  Reporting respectively.
- **Kafka, the outbox and the inbox** — services call each other over plain HTTP with the
  caller's token. A failed call is retried from the page.
- **Refresh tokens, an audit table, a drug formulary, PDF export, CI, Testcontainers, e2e tests** —
  one 8-hour access token, history rows as the audit trail, free-text medications.
- **gRPC, Dapper, OpenTelemetry, .NET Aspire, a Redis SignalR backplane** — each is a real
  technique, and each is a different project.

## The shape

4 services — Identity, Studies, Imaging, Reporting — behind one YARP gateway, with a Blazor
Server web app. PostgreSQL, one database and schema per service, 9 tables; **no foreign key ever
crosses a schema**. Three synchronous calls cross a service boundary: Studies reads people from
Identity at booking, Imaging tells Studies images arrived, Reporting asks Studies who holds the
lock and tells it when a read is finished.

45 days · 54 tasks · S0 → S4. Progress: `python3 plan/check.py`.
