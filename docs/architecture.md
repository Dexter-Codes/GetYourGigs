# Architecture and rules

## Deployment boundary

```text
React browser ───────┐
                    ├── HTTPS /api ── ASP.NET Core monolith ── PostgreSQL
Native MAUI app ────┘                         │
                                     persistent image storage
```

One deployable backend owns identity, authorization, business decisions, persistence, and activity. React is built into the API's web root. MAUI uses the shared C# transport client. No client is authoritative for booking, quotas, ownership, or review eligibility.

Services are scoped per request, and DbContext is never shared across concurrent requests. Read queries are no-tracking. Endpoints validate immutable DTOs and return Problem Details on failures. SQL lock statements use interpolated, parameterized EF APIs. Schema changes are tracked EF migrations, not runtime CREATE TABLE statements.

## Identity and context

Accounts are keyed by a server-issued UUID, with a unique normalized verified email. Email challenges are HMAC-hashed, expiring, rate-limited, attempt-limited, and consumed transactionally. Google ID tokens are validated server-side against the configured audience and require a verified email. The original prototype's name/email identity pointer is not authentication and was not carried over.

PreferredMode is a view preference, not an authorization role. A hirer can also create a talent profile, apply to other people's gigs, and switch context. Every protected endpoint derives ownership from the signed JWT subject, never a caller-supplied owner ID.

The web keeps the 12-hour access token in sessionStorage; MAUI uses SecureStorage. There is no silent refresh or per-session server-side revocation in this MVP. Production should add refresh/revocation before requiring long-lived login sessions. Google's native path reuses browser login with a proof-key-bound handoff: the native verifier never leaves the app until token exchange; the browser only receives its SHA-256 challenge; the callback contains an expiring code, not a token.

## Booking consistency

Application mutations acquire **applicant account row → target gig row** locks, inside the same PostgreSQL transaction. The account lock serializes the daily cap and decisions across gigs/hirers for that applicant. The gig lock serializes applicants competing for the same position. These are database locks, so protection holds across API processes rather than depending on an in-process semaphore. A partial unique index provides an additional single-booked-applicant-per-gig invariant.

Booking writes the winner, gig state, sibling waitlists, conflicting application cancellations, and activity records atomically. The application count is not checked only in the client. The day boundary is **Asia/Kolkata**, and all submitted applications—including withdrawn ones—count toward that day.

Dates are nullable `DateOnly` values. Known same-day bookings conflict; an undated booking cannot assert calendar availability and should be agreed directly. Dates cannot be changed after applications exist; close/repost to change the schedule. A booked applicant may withdraw before/on the scheduled date; the gig reopens, and waitlisted people can be considered. Closed/completed and terminal applications cannot be arbitrarily moved back to active states.

## Completion and reviews

The hirer must explicitly confirm that a booked gig happened. Future dated gigs cannot be completed early. Passing a date does not fabricate completion. Completion changes the winner to Completed and remaining active/waitlisted applications to NotSelected. Only the owning hirer can review a completed winning application, once, enforced by a unique database index. Public ratings are aggregates of these reviews, and absent reviews remain unrated.

## Messaging and updates

Conversations have a canonical participant pair plus optional gig context. Creation resumes an existing matching thread. Only those participants can read or send messages; account UUIDs alone grant no access. Message records are separate rows so simultaneous replies do not overwrite a JSON reply array. Reads return the latest 200 messages; historical message pagination is a future addition, and all messages remain stored. Updates show the latest 100 activity events and can be marked read. No fabricated incoming replies or sample activity are generated.

## Media and operational limits

Both editors support up to five photos. Uploads require authentication, cap requests/files at approximately 6/5 MB, verify JPEG/PNG/WebP signatures, generate filenames, reject executable SVG/HTML, and serve with nosniff headers. Images and remote HTTPS URLs are public portfolio media. File signatures do not replace malware scanning or moderation; object-storage quotas, abandoned-upload cleanup, content moderation, abuse handling, account deletion/export, and deployment monitoring are later production work.

Search uses PostgreSQL ILIKE substring matching with pagination. Category/city/status and activity/application lookup indexes are declared in the model. Trigram/full-text search is unnecessary for the current MVP; add it only with real query-plan/workload evidence. Sample photos come from Unsplash, fonts are locally bundled on mobile with their open font licenses, and no runtime CDN is needed for the application logic.

The optional map, geocoding, voice notes, payments, push notifications, advanced portfolio/business fields, and legacy Supabase import are outside this first implementation. The existing Supabase project was left untouched.

## Code organization and mobile MVVM

API route registration is grouped into Auth, Account, Catalog, Booking, Messaging,
and Upload endpoint classes. `ApiEndpoints` composes those groups. Handlers bind
contracts and call interface-backed services; booking transactions and lock order
remain inside the services. Expected uniqueness/foreign-key conflicts return 409;
unexpected database failures are logged and return a sanitized 500 Problem Details
response with a trace identifier.

Each mobile screen has its own view-model file. Page code-behind contains only
XAML initialization and constructor-injected binding context. `App` and platform
entry points retain the framework startup code. `PageCommands` connects page
visibility to commands through attached properties. It contains no marketplace
policy. Compiled bindings and the existing `SafeAreaEdges="All"` page style remain
the defaults.

View models depend on `INavigator`, `IUserDialogs`, `IPhotoPicker`,
`IServerConfiguration`, and `IBrowserSignIn` where needed. Platform implementations
own navigation, alerts, media selection, preferences, and browser authentication.
The shared `MediaEditorViewModel` owns photo drafts/upload commands for both editors.
Server switching has one implementation and clears the previous session before
changing the transport address. `AppSession` owns the current account, while
`GigsApiClient` remains a transport client rather than a business-policy layer.
Pages/view models are transient; shared session and platform services are singletons.

Chat polling is owned by the view model, cancels its in-flight read when the page
closes, and serializes refreshes with sending so a refresh cannot silently discard
a send action. Chat colors/alignment are XAML triggers rather than view-model UI
types. Confirmation commands can be tested without displaying native dialogs.

The .NET test project links the platform-independent view-model source and uses
HTTP handlers and platform-service substitutes. These tests cover confirmation,
current application status, upload limits and stream cleanup, and chat cancellation.
They do not replace native navigation, keyboard, photo-picker, or sign-in tests.
Native builds validate C# and compiled XAML; runtime dependency resolution,
physical-device behavior, and production deployment still require separate validation.

## Backend and React maintainability

`Program.cs` describes the HTTP pipeline; `Hosting/MarketplaceServices.cs` owns
core service, authentication, validation, and rate-limiter registration. Service
interfaces live beside their implementations in named files. `BookingService`
keeps its database locks and commit in the public operation; the private booking
helper groups conflict cancellations, waitlisting, and activity creation without
opening an independent transaction.

Catalog reads project directly into response records. The shared mapping
expressions calculate ratings and application counts in SQL for queries, while
mutation responses map the tracked graph already loaded by the operation. This
avoids loading complete review/application collections for catalog reads. No
schema migration or API contract change is needed.

The React source separates `pages/`, `flows/`, and reusable `components/`.
`app/useMarketplace.ts` owns application coordination; `app/AppDialog.tsx` renders
its typed dialog state. `api.ts` handles HTTP and session tokens, while
`hooks/useResource.ts` owns cancellable reads and `hooks/useInbox.ts` owns
conversation selection, polling, and per-conversation drafts. Effect cleanup
aborts obsolete reads and prevents their results from replacing current data.
An unauthorized response only expires the session that sent that request.

Forms guard against duplicate pending submissions. Chat send completion clears
only the submitted draft, preserving edits or drafts in another conversation.
TypeScript checks unused locals and parameters during the production build;
Vitest discovers both `.test.ts` and `.test.tsx` files.
