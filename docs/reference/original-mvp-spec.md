# Get Your Gigs — Technical Spec (current build)

**Purpose of this document:** this describes the *actual current implementation* — a single self-contained HTML file — so a coding agent can make UI/UX changes safely without breaking the app's conventions. This is not a proposal; it's a map of what already exists.

---

## 1. Architecture at a glance

- **One file.** The entire app — HTML, CSS, and JavaScript — lives in a single `.html` file. There is no build step, no bundler, no npm packages, no framework (no React/Vue/etc). It's vanilla JS.
- **No backend server.** The browser talks directly to **Supabase's REST API (PostgREST)** via `fetch()`, using a public anon key. Security is enforced by Supabase Row Level Security policies (currently permissive — this is a prototype, not production-hardened).
- **Rendering model:** a single global `state` object holds all app data and UI state. A `render()` function rebuilds the *entire* `#app` div's `innerHTML` from scratch on every state change, based on `state.view`. There is no virtual DOM, no component tree, no diffing — every render is a full string rebuild.
- **Why this matters for making changes:** you cannot "update just one component." Any change to what's on screen means editing the JavaScript function that returns the HTML string for that view, or the shared CSS. There's no compiler to catch mistakes — a broken template literal or unbalanced tag fails silently or breaks the whole page.

---

## 2. File anatomy

```
<head>
  <style>          — ALL CSS lives here, one block, ~260 lines
  <script src=...> — Leaflet (map) + Google Identity Services, both loaded
                      from allowed CDNs, both optional/non-blocking if they fail
</head>
<body>
  <div id="app">   — everything renders into this one div
  <script>         — ALL JavaScript lives here, one block, ~2700 lines
    - Config constants (Supabase URL/key, Google Client ID, domain lists)
    - Data mapping functions (toDb*/fromDb* — camelCase JS ↔ snake_case Postgres)
    - Database helper functions (sbSelect/sbInsert/sbUpdate/sbDelete/sbUpsert)
    - Business logic (booking cascades, date parsing, matching, etc.)
    - `state` object (single source of truth)
    - `render()` — the view dispatcher
    - One `render<ViewName>()` function per screen
    - Event delegation: two listeners on `document` (`click`, `submit`, `input`, `change`)
    - `boot()` — runs once on page load
  </script>
</body>
```

**No CDN dependency for the app's own logic.** Only Leaflet (map tiles) and Google Identity Services (sign-in button) are external, and both degrade gracefully to "just don't show that feature" if they fail to load — the app must never depend on an external script loading successfully for its core functionality. This was a real bug once (a broken CDN reference silently killed the entire app); don't reintroduce that pattern.

---

## 3. Design system (for UI/UX work specifically)

### Color tokens (CSS custom properties, defined once in `:root`)
```
--ink          near-black text
--ink-soft     secondary/muted text
--paper        page background (warm off-white)
--paper-raised card/surface background (white)
--line         border color
--line-soft    faint border/divider
--teal         primary interactive accent (links, active chips, focus states)
--teal-soft    light tint of teal (badges, tag backgrounds)
--signal       secondary accent — coral, used for primary CTAs
--signal-ink   dark text-on-signal color
--danger       error/destructive red
--danger-soft  light tint of danger
--radius       default border-radius value
```
**Change the palette by editing these ~13 values once** — every component references them, nothing is hardcoded in hex elsewhere (if you find a hardcoded color during editing, that's a smell — convert it to a token).

### Typography
- Display/headings: **Fraunces** (serif, via Google Fonts)
- Body/UI: **Manrope** (sans-serif)
- Labels/mono accents (category tags, timestamps): **IBM Plex Mono**

### Core component classes (reuse these, don't invent new one-off styles)
| Class | What it is |
|---|---|
| `.btn`, `.btn-outline`, `.btn-signal`, `.btn-danger`, `.btn-ghost`, `.btn-sm` | Button variants |
| `.stub` | The "ticket stub" card — used for both talent profiles AND gig postings in every grid, this is the app's signature visual motif |
| `.form-card` | White card wrapper for any form-based screen |
| `.chip-toggle`, `.category-chip` | Pill-style toggle buttons (used for category filters, role pickers, availability status, etc.) |
| `.status-tag` + `.status-{value}` | Colored status badges (applied/booked/completed/etc — see Section 6) |
| `.field` | Form field wrapper (label + input + hint text) |
| `.section-head` | Page title + count/meta row, used at the top of every list view |
| `.empty-state` | Dashed-border placeholder for "nothing here yet" |
| `.context-toggle` | The Hiring/Working pill switch in the nav |
| `.avail-badge`, `.rating-badge` | Small inline status/rating indicators on cards |

### Responsive breakpoints
- `820px` — hero/two-column layouts collapse to single column
- `680px` — main mobile breakpoint: nav condenses, grids drop to 2 then 1 column, forms tighten padding, map/hero sizing reduces
- `420px` — grids force to single column regardless of content width

All breakpoints live in one consolidated media-query block near the end of the `<style>` section — add new mobile-specific rules there rather than scattering `@media` blocks throughout, so they stay auditable in one place.

---

## 4. Rendering & state model

```js
let state = { view: 'landing', me: null, profiles: [], postings: [], applications: [],
               inquiries: [], reviews: [], bookmarks: [], filters: {...}, ... };

function render() {
  // 1. picks the right renderX() function based on state.view
  // 2. rebuilds #app's entire innerHTML
  // 3. only replays the page-transition fade animation if state.view actually changed
  //    (NOT on every re-render — this was a real flicker bug, see Section 7)
  // 4. runs post-render hooks: photo preview thumbnails, Google button, city datalist
}
```

- **To change what a screen shows:** find its `render<Name>()` function (one per view — see the table in Section 6) and edit the HTML template string it returns.
- **To add a new screen:** add a `case` to the `render()` switch, write a new `render<Name>()` function, add navigation to it (a `data-action="go" data-view="yourNewView"` element somewhere).
- **State updates never happen inside a render function** — render functions are pure (state) → HTML string. All state mutation happens in the event handlers (Section 5).

---

## 5. Event handling (no inline `onclick`, ever)

Every interactive element uses **event delegation** via `data-action` (clicks) and `data-form` (submits) attributes — never inline JS handlers:

```html
<button data-action="editProfile" data-id="123">Edit</button>
```
```js
document.addEventListener('click', async function(e){
  const el = e.target.closest('[data-action]');
  if(!el) return;
  const action = el.dataset.action;
  if(action === 'editProfile'){ /* ... */ }
  else if(action === '...') { /* one big if/else-if chain */ }
});
```

Same pattern for forms via `data-form="formName"` + a `submit` listener switching on `form.dataset.form`.

**Live-search inputs** (Discover search, city filters, the requirement wizard) use a **debounced** `input` listener — they update `state` immediately but delay the actual re-render ~200ms so rapid typing doesn't cause a full-page rebuild per keystroke. Follow this pattern for any new live-filtering UI; don't call `render()` synchronously on every keystroke.

**When adding a new clickable/submittable element:** always add its handler in the same `if/else-if` chain — a `data-action` or `data-form` with no matching handler is a silent no-op bug (this has happened multiple times during development; always grep for the action name after adding it to confirm a handler exists).

---

## 6. View reference

| `state.view` | Function | Shown to | Purpose |
|---|---|---|---|
| `landing` | `renderLanding` | Signed out | Hero, category chips, hiring/working search |
| `preview` | `renderPreview` → `renderPreviewTalent` / `renderPreviewPostings` | Signed out | Gated preview (3 visible, rest blurred) before sign-in |
| `identity` | `renderIdentityForm` | Signed out | Sign-in/sign-up (Google or email) |
| `wizard` | `renderWizard` | Signed out | 3-step "what are you looking for" buyer flow |
| `quickProfileForm` | `renderQuickProfileForm` | New talent | 5-field fast onboarding |
| `profileLive` | `renderProfileLive` | New talent | "You're live" + profile completeness |
| `dashboard` | `renderDashboard` → `renderDiscover` / `renderGigsBrowseFeed` | Signed in | Home page, content depends on Hiring/Working toggle |
| `gigs` | `renderGigs` → `renderProviderGigs` / `renderMyApplications` | Signed in | "My stuff" page, also toggle-dependent |
| `myApplicants` | `renderMyApplicants` | Hiring | All applicants across all your postings, filterable |
| `postingForm` | `renderPostingForm` | Hiring | Create/edit a gig posting |
| `postingDetail` | `renderPostingDetail` | Anyone | Full gig detail page |
| `applyToGig` | `renderApplyToGig` | Working | Application form for a specific gig |
| `applicationSent` | `renderApplicationSent` | Working | Confirmation screen |
| `detail` | `renderDetail` | Anyone | Full talent profile page (bio, portfolio, reviews) |
| `inquiry` | `renderInquiryForm` | Anyone | Start a new message thread |
| `inbox` / `thread` | `renderInbox` / `renderThread` | Signed in | Message list / single conversation |
| `myProfile` | `renderMyProfile` | Signed in | Account + (toggle-dependent) either "my listing" or "hiring activity" |
| `saved` | `renderSaved` | Signed in | Bookmarks, toggle-dependent (saved talent vs. saved gigs) |
| `updates` | `renderUpdates` | Signed in | Activity feed (status changes, new messages) |
| `reviewForm` | `renderReviewForm` | Hiring | Leave a review after marking a gig complete |

**The Hiring/Working toggle is the most important cross-cutting concept in the UI.** `getViewContext()` returns `'hiring'` or `'working'` for the current user, and several views (`dashboard`, `gigs`, `myProfile`, `saved`) branch their entire content on it. If you're asked to change "what happens in hiring mode" or "working mode," start by finding `getViewContext()` calls, not by assuming it's tied to how the account originally signed up (`state.me.role`) — those are deliberately decoupled; any account can act as either.

---

## 7. Known constraints — read before making changes

- **No `localStorage`/`sessionStorage` inside Claude.ai's artifact preview** — it's disallowed there and silently unavailable. The app's identity-persistence functions (`localGet`/`localSet`/`localDelete`) already handle this correctly: they try Claude's `window.storage` API first (only exists inside Claude.ai), then fall back to real `localStorage` (only reachable once self-hosted, where it works fine). **Don't add new direct `localStorage` calls elsewhere** — route any new persistence need through the existing `localGet`/`localSet` pattern.
- **External scripts only load from `cdnjs.cloudflare.com`** inside the Claude.ai preview sandbox. Adding a `<script src="...">` from any other CDN (jsdelivr, unpkg, etc.) will silently fail to load there — this has caused a real production bug before. If a library is needed, check it's on cdnjs first, and always code defensively (`typeof Library !== 'undefined'`) so a failed load degrades instead of crashing.
- **Google Sign-In cannot be tested inside Claude.ai's chat preview** — it needs a stable registered origin, which the preview doesn't have. It only works once deployed to a real domain with that domain added in Google Cloud Console.
- **No test suite, no linter, no CI.** Every change must be manually verified before shipping:
  1. Extract the `<script>` contents and run `node -c` on them (catches syntax errors)
  2. Run an HTML tag-balance check (an `HTMLParser`-based script works well — unbalanced tags from a bad edit are a very common and very silent failure mode)
  3. Cross-check that every `data-action`/`data-form` referenced in markup has a matching handler (grep both lists, diff them)
  4. Where feasible, actually execute the relevant render function in Node with mocked `fetch`/`document` and representative data, and check the output string for `undefined`/`NaN` or missing expected content — don't rely on "it parses" as proof it's correct
- **Geocoding (Nominatim) is rate-limited and called client-side** — it's used sparingly (once per profile/posting save, best-effort, silently skipped on failure) and must stay that way; don't add features that call it in a loop or on every keystroke.

---

## 8. Data model (Supabase Postgres tables)

`profiles`, `gig_postings`, `gig_applications`, `inquiries`, `reviews`, `bookmarks`, `stakeholders` (user accounts). Each has a `toDb*`/`fromDb*` pair in the JS mapping camelCase app fields to snake_case Postgres columns — when adding a new field, add it in **three** places: the SQL table (a migration), the `toDb*` function, and the `fromDb*` function, or it will silently vanish on save/reload.

---

## 9. Suggested workflow for a UI/UX change

1. Identify the view (Section 6) and open its `render<Name>()` function.
2. Identify the reusable component classes involved (Section 3) — prefer editing/extending an existing class over introducing a new one, to keep visual consistency.
3. Make the change directly in the HTML template string (it's string concatenation, not JSX — watch your quote escaping).
4. If the change is visual/layout, check whether it needs a rule in the mobile breakpoint block too.
5. Run the four-step verification in Section 7 before considering it done.
