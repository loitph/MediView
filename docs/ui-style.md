# MediView UI style

The visual contract for every page in `MediView.Web`. It is extracted from the hi-fi mockup
`MediView UI - MVP.dc.html` (eight pages, nine frames). The **look** in this file is mandatory. The
mockup's **copy and features** are not: where they describe something the plan cut, the plan wins
(see "What the mockup shows that we do not build").

Desktop only, designed at 1440 px wide. Light theme everywhere except the DICOM viewport, which
is dark.

## 1. Rules that are not negotiable

1. **Colours come from tokens.** Every colour, radius and font in a `.razor` or `.razor.css` is a
   `var(--mv-*)` from section 2. Never write a raw hex value in a component.
2. **Two typefaces.** IBM Plex Sans for text. IBM Plex Mono for identifiers, times, numbers in
   tables, column headers and eyebrows. Nothing else.
3. **Status labels are exact:** `To do`, `In progress`, `Re-diagnosis`, `Done`. Same words, same
   case, same colours on every page. Never "Pending", "Open", "Completed", "Reread".
4. **One shell.** Every signed-in light page uses the sidebar + top bar shell (section 4). The
   viewport is the only page without it.
5. **Cards have borders, not shadows.** A white card on the `--mv-bg` page with a 1 px
   `--mv-border` line and a 9 px radius. Shadows only appear on the focus ring and the active
   segmented tab.
6. **Navy is the primary action.** One primary (navy) button per panel. Teal is the accent
   (links, focus, active step, the import action), not a second primary.
7. **Semantic markup.** The mockup draws everything with `div`s. In Razor use `button`, `a`,
   `label` + `input`, `table` / `thead` / `th`, `nav`, `header`, `main`, `section`.

## 2. Design tokens

Copy into the `:root` of `wwwroot/app.css`. Component styles reference these names only.

`app.css` also holds the shared classes every page uses: `mv-page` (with `__main` and
`__side--summary|target|form`), `mv-card`, `mv-table`, `mv-button--primary|secondary|accent|create`
(plus `--block`, `--tall`, `--compact`), `mv-field`, `mv-input`, `mv-form-grid`, `mv-callout--info|warning|danger`,
`mv-segmented`, `mv-avatar`, `mv-eyebrow`. A page's own `.razor.css` only holds what is unique to that
page. Shell pages start with `<TopBar Title="…" Meta="…">`; sign in and register use `@layout AuthLayout`.

```css
:root {
    --mv-font-sans: 'IBM Plex Sans', system-ui, sans-serif;
    --mv-font-mono: 'IBM Plex Mono', ui-monospace, monospace;

    --mv-ink: #14284a;
    --mv-text: #2c3a4f;
    --mv-text-secondary: #4a5a72;
    --mv-text-label: #3d4d66;
    --mv-text-muted: #7b8a9f;
    --mv-text-faint: #93a1b5;
    --mv-text-disabled: #a3aebe;

    --mv-accent: #0d7d8c;
    --mv-accent-hover: #0a5f6b;
    --mv-accent-strong: #0d6d7c;
    --mv-accent-bright: #25b3c4;
    --mv-accent-ring: rgba(13, 125, 140, .12);

    --mv-bg: #f1f3f7;
    --mv-surface: #fff;
    --mv-surface-subtle: #f8fafc;
    --mv-surface-input: #fbfcfe;
    --mv-surface-track: #e4e8ef;

    --mv-border: #dfe4ec;
    --mv-border-input: #ccd4e0;
    --mv-border-chip: #dbe1ea;
    --mv-divider: #eef1f5;
    --mv-divider-row: #f3f5f9;

    --mv-sidebar-bg: #14284a;
    --mv-sidebar-text: #a4b5cb;
    --mv-sidebar-section: #5d738f;
    --mv-sidebar-active-bg: rgba(37, 179, 196, .16);
    --mv-auth-pitch-text: #9fb2cd;

    --mv-role-patient: #1d7f4e;
    --mv-role-doctor: #1f5fa8;
    --mv-role-admin: #a1660a;
    --mv-avatar-soft-bg: #e7edf7;

    --mv-status-todo-bg: #e8ecf2;
    --mv-status-todo-fg: #5b6b81;
    --mv-status-todo-dot: #94a3b8;
    --mv-status-progress-bg: #fdf3dc;
    --mv-status-progress-fg: #a1660a;
    --mv-status-progress-dot: #d89e1a;
    --mv-status-rediagnosis-bg: #fbe7e1;
    --mv-status-rediagnosis-fg: #b0402c;
    --mv-status-rediagnosis-dot: #b0402c;
    --mv-status-done-bg: #e2f2e8;
    --mv-status-done-fg: #17724a;
    --mv-status-done-dot: #1d7f4e;
    --mv-status-done-border: #bfe0cc;

    --mv-priority-routine-bg: #eef1f5;
    --mv-priority-routine-fg: #6b7c93;
    --mv-priority-urgent-bg: #fdf3dc;
    --mv-priority-urgent-fg: #a1660a;
    --mv-priority-stat-bg: #fbe7e1;
    --mv-priority-stat-fg: #b0402c;

    --mv-lock-mine: #1d7f4e;
    --mv-lock-other: #b0402c;
    --mv-lock-free: #c3ccd8;

    --mv-success: #1d7f4e;
    --mv-danger: #b0402c;
    --mv-warning: #a1660a;

    --mv-info-bg: #eef6f7;
    --mv-info-border: #c5e0e4;
    --mv-info-text: #3d5c63;
    --mv-warn-bg: #fdf9ee;
    --mv-warn-border: #f0dcae;
    --mv-warn-text: #6d5316;
    --mv-danger-bg: #fcf1ee;
    --mv-danger-border: #f0c9bf;
    --mv-danger-text: #7d3324;

    --mv-button-disabled-text: #b3bdcb;
    --mv-segment-inactive: #6b7c93;
    --mv-segment-shadow: 0 1px 2px rgba(20, 40, 74, .1);

    --mv-radius-frame: 10px;
    --mv-radius-card: 9px;
    --mv-radius-control: 7px;
    --mv-radius-small: 6px;
    --mv-radius-tag: 4px;
    --mv-radius-pill: 20px;

    --mv-sidebar-width: 224px;
    --mv-topbar-height: 62px;
    --mv-page-padding: 26px 30px;

    --mv-focus-ring: 0 0 0 3px var(--mv-accent-ring);
}
```

Fonts load once, in `App.razor`:

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:wght@400;500;600;700&family=IBM+Plex+Mono:wght@400;500;600&display=swap">
```

### Dark tokens (viewport only)

Scoped to the viewport root (`.mv-viewport`), never to `:root`.

| Token | Value | Used for |
|---|---|---|
| `--mv-dark-bg` | `#0f1319` | page behind everything |
| `--mv-dark-chrome` | `#171c24` | top bar, tool rail, side panel, frame footer |
| `--mv-dark-rail` | `#131820` | series strip |
| `--mv-dark-border` | `#262d38` | chrome dividers |
| `--mv-dark-input` | `#0f141b` | text areas, medication list |
| `--mv-dark-input-border` | `#2b3340` | input borders, unselected options |
| `--mv-dark-control-border` | `#333c4a` | secondary buttons |
| `--mv-dark-text` | `#eef2f7` | titles |
| `--mv-dark-text-body` | `#c9d3df` | input text, body |
| `--mv-dark-text-secondary` | `#8b97a6` | meta, back link |
| `--mv-dark-text-label` | `#6d7a8b` | eyebrows |
| `--mv-dark-text-disabled` | `#5c6878` | disabled controls |
| `--mv-accent-bright` | `#25b3c4` | primary button fill (text `#06222a`), active tool, slider |

Status and lock pills on dark use the tinted form: background at 14–16 % alpha of the hue, a
border at 35–42 %, and a lighter text colour (`#e2ab3c` in progress, `#5fd39c` lock mine,
`#eb9280` locked by another).

## 3. Typography

| Role | Font | Size / weight | Colour | Notes |
|---|---|---|---|---|
| Top bar page title | Sans | 17 / 600 | `--mv-ink` | |
| Top bar meta | Mono | 12.5 / 400 | `--mv-text-muted` | after the title, `·` separated |
| Card title | Sans | 14.5 / 600 | `--mv-ink` | |
| Body | Sans | 13.5–14 / 400 | `--mv-text` | line-height 1.6; report text 1.7 |
| Secondary text | Sans | 13 / 400 | `--mv-text-secondary` or `--mv-text-muted` | |
| Small print, helper | Sans | 12–12.5 / 400 | `--mv-text-muted` | line-height 1.55–1.6 |
| Field label | Sans | 12.5 / 600 | `--mv-text-label` | sentence case |
| Eyebrow, column header | Mono | 10.5–11 / 600 | `--mv-text-faint` | UPPERCASE, letter-spacing .06–.08em |
| Identifier, time, count | Mono | 12–13.5 / 500–600 | `--mv-ink` / `--mv-text-secondary` | `MRN-1042`, `STU-001`, `09:00` |
| KPI number | Sans | 30 / 600 | `--mv-ink` | letter-spacing -0.02em |
| Sidebar brand | Sans | 18 / 700 | `#fff` | with a 4 × 22 px `--mv-accent-bright` bar |
| Sidebar section | Mono | 10.5 / 600 | `--mv-sidebar-section` | `PATIENT`, `DOCTOR`, `ADMIN` |

Use `-webkit-font-smoothing: antialiased` on `body`. Long prose gets `text-wrap: pretty`.

## 4. Layout

### App shell (all light pages except sign in)

```
┌──────────┬────────────────────────────────────────────────┐
│ sidebar  │ top bar  62px · white · bottom border           │
│ 224px    ├────────────────────────────────────────────────┤
│ navy     │ content  padding 24–28px 30px · bg --mv-bg      │
│          │   main column (flex:1)   |  side column 350–450 │
│ user     │                                                 │
└──────────┴────────────────────────────────────────────────┘
```

- **Sidebar**: `--mv-sidebar-bg`, padding 22px 0, gap 26px. Brand at top; a mono section label
  for the role; nav items 13.5 px, padding 10px 12px, radius 6 px, `--mv-sidebar-text`. Active item:
  `--mv-sidebar-active-bg`, white text, weight 600. User block pinned to the bottom: 32 px circle
  avatar in the role colour with initials, name 13 px white, mono sub-line (MRN for a patient,
  department for a doctor).
- **Top bar**: 62 px, white, `border-bottom: 1px solid var(--mv-border)`, padding 0 30px. Left:
  title + optional mono meta. Right: filters, search (34 px high) or one primary action, or the
  page's status pill.
- **Content**: two columns are the norm. The main column is `flex: 1; min-width: 0`; the side column
  is fixed (352 px summary, 372 px target, 452 px form) and holds the summary, the primary action and
  info callouts. Column gap 22–24 px; vertical gap between cards 16–20 px.

### Sign in / register

No shell. A 600 px navy panel on the left (brand, one-line pitch at 29 / 500 white, three bullet
lines in `--mv-auth-pitch-text` with teal dots), and a centred 392 px form column on `--mv-bg` on the right.
Sign in / Register is a segmented control.

### Viewport (dark)

Full screen, no sidebar. 56 px top bar (back link, patient + mono meta, status pill, lock pill,
avatar), then: 74 px tool rail, 132 px series strip, black canvas with mono overlays in the four
corners (`rgba(255,255,255,.55)`, 11 px), 52 px frame footer with slider, and a 392 px report panel
on the right with its actions pinned to the bottom. When another doctor holds the lock, add a 52 px
red-tinted READ-ONLY banner under the top bar, dim the tool rail to 45 % and replace the editor with
the lock-owner card.

## 5. Components

### Card

```css
.mv-card { background: var(--mv-surface); border: 1px solid var(--mv-border); border-radius: var(--mv-radius-card); overflow: hidden; }
.mv-card__header { padding: 16px 24px; border-bottom: 1px solid var(--mv-divider); display: flex; justify-content: space-between; align-items: center; }
.mv-card__body { padding: 20px 24px; display: flex; flex-direction: column; gap: 16px; }
```

Header right side holds a mono meta (`4`, `RP-0114`, `24 instances · 1 series`) or one secondary button.

### Data table

- Header row: `--mv-surface-subtle`, padding 12px 24px, mono 10.5 / 600 uppercase, `--mv-text-faint`.
- Body rows: padding 16–18px 24px, `border-bottom: 1px solid var(--mv-divider-row)`, vertically centred.
- No zebra stripes, no vertical lines. A selected row gets a 3 px `--mv-accent` left border and
  `#f4f8fb` background.
- Primary cell: name in Sans 14 / 600 with a mono 11.5 sub-line (MRN, licence).
- Action column is right-aligned; one small button per row.

### Status badge

Pill: radius 20 px, padding 5px 12px (4px 10px in compact lists), Sans 12 / 600, background and
text from the status pair. In a page header the Done pill also gets a `--mv-status-done-border` border.

| Status | Background | Text | Dot (KPI, timeline) |
|---|---|---|---|
| To do | `--mv-status-todo-bg` | `--mv-status-todo-fg` | `--mv-status-todo-dot` |
| In progress | `--mv-status-progress-bg` | `--mv-status-progress-fg` | `--mv-status-progress-dot` |
| Re-diagnosis | `--mv-status-rediagnosis-bg` | `--mv-status-rediagnosis-fg` | `--mv-status-rediagnosis-dot` |
| Done | `--mv-status-done-bg` | `--mv-status-done-fg` | `--mv-status-done-dot` |

Build it once as a `StatusBadge` component that takes the status enum; pages never pick colours.

### Priority tag

Square-ish tag, radius 4 px, padding 4px 8px, mono 10.5 / 600 uppercase, letter-spacing .05em:
`ROUTINE`, `URGENT`, `STAT` from the priority tokens.

### Lock indicator

7 px dot + 12.5 px text. `You` in `--mv-lock-mine`; `Dr. D` (the owner's name) in
`--mv-lock-other`; `Free` / `Released` with a `--mv-lock-free` dot and muted text; `No images yet`
fully faded.

### Buttons

| Kind | Fill | Border | Text | Use |
|---|---|---|---|---|
| Primary | `--mv-ink` | none | white, 14 / 600 | the one main action of a panel |
| Secondary | white | `--mv-border-input` | `--mv-ink` | alternative actions, table row actions |
| Accent | `--mv-accent` | none | white | import |
| Confirm-create | `--mv-success` | none | white | creating an account |
| Disabled | `--mv-surface-subtle` | `--mv-surface-track` | `--mv-button-disabled-text` | e.g. "Awaiting images" |

Full-width panel buttons are 44 px high (46 px on sign in), radius 7 px. Row and header buttons are
compact: padding 7px 14px, radius 6 px, 12.5 / 600. Labels are verbs in sentence case: "Confirm
booking", "Create doctor", "Finalize & release lock".

### Form fields

- Label above the field, 12.5 / 600 `--mv-text-label`, gap 6 px.
- Field: 40 px high (44 px on sign in), radius 7 px, `1px solid var(--mv-border-input)`, padding 0
  12–14px, 13.5–14 px text, background white or `--mv-surface-input` inside a form card.
- Focus: `border: 1.5px solid var(--mv-accent); box-shadow: var(--mv-focus-ring);`.
- Two-column grid with 13 px gap; email spans both columns.
- Text areas: min-height 66–76 px, padding 12px 14px, line-height 1.6.
- Validation and failures: `--mv-danger` text under the field; a page-level failure uses a
  danger-tinted callout, not a Bootstrap alert.

### Segmented control

Track `--mv-surface-track`, radius 7 px, padding 4 px, gap 4 px. Active segment white, radius 5 px,
`box-shadow: var(--mv-segment-shadow)`, 13.5 / 600 ink. Inactive 13.5 / 500 `--mv-segment-inactive`.

### Stepper

24 px numbered circles joined by 44 px 1 px lines (`--mv-border-input`). Done or current:
`--mv-accent` circle, white number, label 13 / 600 ink. Upcoming: `--mv-surface-track` circle,
`#8a98ad` number and label at weight 500.

### KPI tile

White card, padding 18px 20px, 9 px rounded square dot in the status colour + mono 11 / 600
uppercase label, then the number at 30 / 600. Tiles sit in a row with a 14 px gap, equal width.

### Key-value row

`display: flex; justify-content: space-between; align-items: baseline`. Key 13 px muted, value
13.5 / 600 ink. Used in the booking summary, import target and lock facts. Separate groups with a
1 px `--mv-divider` line.

### Callout

Padding 16px 18px, radius 9 px, a 7 px dot aligned to the first line, text 12.5 / 1.6.

- **Info** (`--mv-info-*`, dot `--mv-accent`): explains what the page does next.
- **Warning** (`--mv-warn-*`, dot `--mv-warning`): consequences the user should weigh.
- **Danger** (`--mv-danger-*`, dot `--mv-danger`): a page-level failure, such as a rejected sign in
  or a request the API refused.

### Timeline

Horizontal. Each step: 13 px dot, a 2 px bar to the next step, label 13.5 / 600, mono 11.5 faint
time line (`12 Mar · 08:52 · Dr. B`). Reached steps use `--mv-status-done-dot`; the last step has no
bar.

### Slot picker

Five day cells (mono 11 day, 19 / 600 date, 11.5 teal "6 free"), then a 4-column grid of slot
buttons in mono 14 / 600, padding 13px 0, radius 7 px. Free: white, `--mv-border-input`. Taken:
`--mv-bg` fill, `--mv-border-chip`, `--mv-text-disabled` text, not clickable. Selected: ink fill,
white text. A legend sits under the grid.

### Image placeholder

Until real pixels load, a thumbnail is a 135° stripe of `#20262f` / `#252c36` with a centred mono
caption in `#8b97a6`.

## 6. Copy and data conventions

- Identifiers: `MRN-1042`, `STU-001`, `RP-0114`, `LIC-44120`, always in mono.
- Dates and times: `12 Mar · 09:00`, `Wed 12 Mar`, 24-hour clock. Separate facts with ` · `.
- Ranges: `Mon–Fri · 08:00–16:00 · 30 min` (en dash, no spaces).
- People in placeholders and seed data: `Patient A`, `Dr. B`, `Admin A`. No realistic names.
- Sentence case everywhere except eyebrows, column headers and priority tags.
- Empty states say what will make the thing appear: "no schedule yet — not bookable",
  "Awaiting images".
- PHI rule still applies to the UI: never put patient data into a URL, a log or a page title.

## 7. What the mockup shows that we do not build

The mockup was drawn before the scope cut. Keep its look, drop these (see
`docs/north-star.md`, "Deliberately out of scope"):

| In the mockup | Instead |
|---|---|
| Page 08: audit log and break-glass override; "Audit log" nav item | No page. History rows are the audit trail. |
| "moves live over SignalR" | The page reads status when it loads. |
| "publishes ReportFinalized" / "publishes DoctorRegistered" | Plain HTTP between services; no events on the wire. |
| "Download PDF" on the report | No PDF export. |
| "type to search the formulary" | Medications are free text. |
| "Forgot?" password link | No reset flow. |
| "admin can force a release as an audited break-glass action" | The lock releases on finalize, close or TTL. |
| "every import is written to the audit log" | Import moves the study's history only. |
| Temporary password "shown once" panel | The admin types the initial password in the form. |

When a page task in `plan/` and this file disagree on *what* a page contains, the plan wins. On *how
it looks*, this file wins.
