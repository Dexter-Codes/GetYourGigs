# Verification — 10 November 2026

## Observed

- ASP.NET Core API built with **zero warnings and errors**.
- **16 integration tests passed** using the actual ASP.NET pipeline and an isolated PostgreSQL database. This is not an EF in-memory substitute.
- **Three React regression tests passed**, covering talent-to-gig contract switching, obsolete responses, and Saved mode changes. React TypeScript check and production Vite build passed.
- Native iOS simulator and Android Debug builds passed with **zero warnings and errors**.
- Native Hiring/Working switching was exercised after correcting a refresh feedback loop. The displayed talent/gig headings and actual server-backed card types changed correctly in both directions.
- The iOS app was installed and launched on an iPhone 17 Pro / iOS 26.5 simulator. Visual inspection showed the ivory/coral home, hero, category tiles, search, and server-backed preview listings. It identified a wrapping headline and missing bottom-tab labels; those were corrected before the final build.
- The five-question React onboarding flow was exercised end to end against the real API. After publishing, dismissing the success confirmation still allowed the application form to open without repeating onboarding. Switching Hiring/Working then rendered the correct talent/gig cards. A second labeled QA profile was created for this check.
- Native email-code sign-in was exercised in the simulator using an explicitly labeled local sample account. The app returned to the signed-in Working view and loaded open gigs from the API. This exercised the shared C# HTTP client, verification endpoint, SecureStorage, and account context.
- The React UI was opened against the real listening API, not mocked responses. Verified email-code sign-in, Hiring gigs, the three-step gig creation form, the review step, and a successful post confirmation were observed. The created QA listing is clearly described as a local sample, not a real job offer.
- Anonymous discovery returned **three actual items** with `isPreview=true`, while the authenticated browser displayed all six original sample profiles. The additional QA profile was created afterward. The gated React placeholders do not contain hidden real profile data.
- Migrations were applied to the development PostgreSQL database. The schema includes the mobile sign-in grant migration.

## Integration coverage

1. Anonymous preview cap and private current-account endpoint.
2. The same account switching Hiring/Working with persisted preference.
3. Four concurrent application submissions allowing exactly three.
4. Booking winner, sibling waitlist, and same-day cancellation cascade.
5. Concurrent bookings to the same gig yielding only one winner.
6. Competing hirers unable to double-book one person on the same date.
7. Reviews requiring actual explicit completion, owning-hirer authorization, and uniqueness.
8. Future gigs unable to complete early.
9. Private thread authorization and persisted asynchronous replies.
10. Unauthorized hirers unable to inspect applicants or change status.
11. Private, idempotent bookmarks.
12. Single-use email codes and a limit of five failed verification attempts.
13. Invalid brief and rating validation.
14. Proof-key-bound mobile sign-in handoff and replay rejection.
15. Anonymous/invalid image upload rejection.
16. Shared source-generated C# client compatibility with server DTOs and string enums.

## Limits of the evidence

- No physical iPhone/Android device installation or store submission was performed. Android compilation is not Android device/runtime evidence.
- No real Google OAuth account login or delivery of an email to an external recipient was tested; these require the owner's OAuth/SMTP configuration. Local development codes are visibly labeled and never presented as sent email.
- Mobile Google handoff authorization/proof-key/replay behavior is integration-tested; the full configured provider → system browser → native callback flow has not been exercised on a physical device.
- Native media selection/permissions and full multi-user native booking/messaging flows were not exhaustively exercised in the simulator; the corresponding backend rules are covered by PostgreSQL integration tests.
- Docker, hosted infrastructure, CI execution on GitHub, production TLS/proxy configuration, long-duration sessions, accessibility audits, large-scale load, and visual parity on every screen are not claimed as verified.
- The codebase was initialized as a Git repository, with **no commits or pushes**.

## Hosted testing preparation — 12 November 2026

The earlier Docker limitation above is superseded by these local container checks. Hosted infrastructure remains unverified until the owner creates and configures the services in [deployment.md](deployment.md).

- Final backend regression run: **32 passed, 0 failed**, using isolated PostgreSQL test databases. Coverage includes production email-code visibility, failed delivery rollback, simulated Resend/Supabase requests, public image redirects, and hosting/proxy configuration guards.
- Web: **3 tests passed** and the production build passed.
- The mobile timeout changes compiled successfully for iOS and Android, with zero warnings/errors. This does not establish physical-device cold-start behavior.
- The final Dockerfile built successfully as `getyourgigs:render-testing` on local Docker Desktop. The container published the API and built the React assets from source.
- A Production container applied both existing migrations to a fresh, uniquely named `gyg_tests_*` database. `/health`, `/api/config`, `/api/profiles`, and the React root returned successfully behind simulated HTTPS forwarding. Production development-email fallback was disabled.
- Restarting the container preserved database access and left the migration count unchanged. The smoke harness was corrected to refresh Docker's randomly assigned host port after restart; the subsequent complete run passed.
- An intentionally unreachable database caused migration startup to exit with a nonzero status before the API began listening. All temporary smoke containers, their volumes, and the test network were removed.
- Shell syntax and formatting checks passed. No commits, pushes, cloud provisioning, or external provider calls were performed.

Real Render ingress, Supabase database TLS/storage permissions, Google account sign-in, email recipient delivery, and persistence across an actual Render redeployment still require the post-deployment checklist. Provider HTTP calls in the automated tests are simulated.
