# Retrospective and scope review

Written at the end of S4 (2026-10-06), when the golden path first ran end to end in a browser.
Dates come from the git history.

## Where the time went

| Stretch | Dates | What happened |
| --- | --- | --- |
| Planning and setup | 2026-08-04 → 09-17 | Six weeks of plan, mockups, scaffolding and infrastructure, including Kafka and Kafka UI (09-05) that the 09-17 cut removed again. |
| S1 Sketch | 09-21 → 09-22 | Every service, its schema, the gateway, JWT and the web shell, two days after the cut. |
| S2 Structure | 09-28 | State machine, report aggregate, Redis lock and its concurrency test in one day. |
| S3 Detail | 09-30 → 10-06 | Four vertical slices, API and UI together. |

The build itself was quick once the scope was cut; the planning before the cut was the long part.

## What took longer than planned

- **Cornerstone.js.** The plan called it "the fiddliest part", and it was. Cornerstone3D ships ESM
  that expects a bundler: the DICOM decoder runs in a web worker, the codecs are WASM files found
  through `import.meta.url`, and parts of vtk.js reach for Node built-ins at load time. The fix is
  a small esbuild script in `tools/cornerstone/` that stubs those built-ins and copies the codecs,
  plus `wasmBasePath` at init.
- **Blazor Server interop.** Two failures had no error message at all. Opening several
  `InputFile` streams at once deadlocks the circuit, so uploads now open each file only when it is
  sent. Passing a `string[]` to `InvokeAsync` spreads it into separate JS arguments.
- **The report autosave.** Two fields saving on blur raced each other: the `xmin` concurrency
  check turned the second save into a 500, and a queued save then skipped the impression. Saves
  are now chained, they record exactly what was sent, and a concurrency clash returns `409`.
- **One origin for the browser.** The viewport fetches DICOM from the browser, but CORS was out of
  scope. The web app now forwards `/api/**` to the gateway (recorded in the contract wall).

## Rough edges still open

- **Force release and a second doctor.** P04-T11 says a forced release "lets a second doctor open
  the study", but the north star only lets the *assigned* doctor start a read. Today a second
  doctor stops getting `409 LOCKED` and gets `400 not assigned` instead. Deciding between "any
  doctor may read" and "the admin reassigns" is a real product choice, so it was left open
  rather than guessed.
- **Closing a tab keeps the lock.** Release runs when the page is disposed, which for a closed
  tab happens after the circuit times out and without the doctor's token. The 15-minute TTL is
  the backstop, and the admin can force a release.
- **Compressed DICOM.** The codecs are vendored but only the uncompressed sample has been read.
- **Seed names.** The seeded users are *Ada Admin*, *Dan Doctor* and *Pat Patient*; the UI style
  asks for placeholders like *Patient A* and *Dr. B*.

## What to cut first next time

- **Infrastructure before the first slice.** Kafka was configured before any study existed and
  removed twelve days later. Start with plain HTTP and add transport only when a slice needs it.
- **Breadth in the plan.** The first plan had 105 tasks; the build that works has 54. Cut to the
  golden path before writing the plan, not halfway through it.

## What to add with more time

- The force-release decision above, then a test that two doctors can hand a study over.
- An end-to-end browser test of the golden path. Every bug in the list above was found by
  clicking through the pages, not by the unit tests.
