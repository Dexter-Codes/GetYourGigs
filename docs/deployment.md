# Render + Supabase testing deployment

The deployable unit is one .NET 10 API that also serves React. PostgreSQL and uploaded photos live in a **new Supabase testing project**. This repository includes `render.yaml` and a Docker startup script; no external resource has been created and nothing has been committed or pushed automatically.

## 1. Create the Supabase project

1. Create a Free project, e.g. `getyourgigs-testing`, near the Render region (the Blueprint uses Singapore). Save the database password privately.
2. Turn **Enable Data API** off in the Data API integration settings. The application uses Npgsql directly and its own authentication/authorization, not Supabase Auth or PostgREST. Do this before applying migrations, so app tables aren't accidentally exposed through generated REST endpoints.
3. Select **Connect → Session pooler**, port **5432**. Copy the exact pooler host and username from the dashboard; pooled usernames normally look like `postgres.<project-ref>`. Do not substitute the transaction pooler on port 6543.
4. Form an Npgsql connection string (not a `postgresql://` URL):

   ```text
   Host=<exact-pooler-host>;Port=5432;Database=postgres;Username=<exact-pooler-username>;Password=<database-password>;SSL Mode=VerifyFull;Root Certificate=/etc/secrets/supabase-ca.crt;GSS Encryption Mode=Disable;Maximum Pool Size=10
   ```

   Download the database CA certificate from Supabase Database Settings. In the Render service’s Environment settings, add a Secret File named `supabase-ca.crt` containing the complete PEM certificate (including BEGIN/END CERTIFICATE lines). Render mounts it at `/etc/secrets/supabase-ca.crt`. Add this before the first successful startup; `VerifyFull` needs this CA to validate the Supabase pooler certificate. `GSS Encryption Mode=Disable` avoids optional Kerberos probing; TLS remains enabled.

   Passwords containing connection-string delimiters need Npgsql quoting. Enter the final value directly into Render, never into Git, screenshots, or chat.
5. In **Storage**, create a **public** bucket called `gyg-photos`. Allow `image/jpeg`, `image/png`, and `image/webp`, with a size limit of at least 5 MiB (5,242,880 bytes). Public here means profile/gig images can be viewed by URL, matching the app's existing public listings; do not upload private documents.
6. Under project API keys, obtain the legacy **service_role** JWT key. This integration sends it as both `Authorization: Bearer ...` and `apikey`; do not substitute the publishable/anon key or a new-format `sb_secret_...` key. Store it only on the backend. No anonymous upload policy is needed: the API authorizes upload requests and performs the storage write with the server key.

Existing local data and uploads are not imported automatically. For a fresh test deployment, create new tester accounts and listings. If old database rows are imported later, copy any referenced `/uploads/<filename>` objects into the bucket using exactly the same filenames.

## 2. Configure sign-in

**Google-first testing:** use the existing Google Web OAuth client ID. After Render assigns the service URL, add its exact HTTPS origin to the client's **Authorized JavaScript origins**, and include the testers if the Google consent configuration is restricted to test users. The native app uses the hosted browser handoff already implemented in this repository. Supabase Auth configuration is not used.

**Optional email codes:** create a Resend account, verify a sending domain, and create a sending API key. Set `Email__Resend__ApiKey` and `Email__From`, e.g. `Get Your Gigs <signin@your-domain.example>`. Resend's default test sender is restricted and isn't a substitute for a verified domain for multiple testers. Resend acceptance is not proof of inbox delivery; perform a real recipient test after deployment.

Resend takes precedence over SMTP. Existing local SMTP remains supported, and development without either provider retains the explicitly labeled development code. Production never returns that code. Google-only deployment is supported; email-code requests return a clear 503 until email delivery is configured.

## 3. Review and push the source

Review `.gitignore` and `.dockerignore` before committing. Keep `.env`, `appsettings.Local.json`, `.local`, `.data`, signing credentials, and build output out of Git. The Render Blueprint is safe to commit: secret values are entered in Render, not in YAML. Commit/push to a private GitHub repository yourself when ready.

## 4. Create the Render service

Use **New → Blueprint**, connect the repository and review `render.yaml`. Confirm the service is **Free**, with automatic deploys **Off**. The Blueprint provisions no Render database or disk. It generates `Jwt__Key`; keep this key stable across redeployments so existing sessions and email challenges remain valid.

Enter the prompted values:

| Render environment variable | Value/source |
| --- | --- |
| `ConnectionStrings__Database` | Supabase Session pooler Npgsql connection string |
| `Google__ClientId` | Google Web OAuth client ID |
| `Storage__Supabase__Url` | Supabase project origin, e.g. `https://<ref>.supabase.co` |
| `Storage__Supabase__ServiceRoleKey` | Legacy `service_role` JWT; backend only |

These values are supplied by the Blueprint:

| Variable | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_HTTP_PORTS` | `10000` |
| `Hosting__RenderProxy` | `true` |
| `GYG_MIGRATE_ON_START` | `true` |
| `Storage__Provider` | `Supabase` |
| `Storage__Supabase__Bucket` | `gyg-photos` |
| `Jwt__Key` | Generated secret |

For email codes, add `Email__Resend__ApiKey` and `Email__From`. If using only Resend, the Google ID can be omitted when configuring a service manually; the starter Blueprint assumes Google-first testing.

For manual service creation: Language **Docker**, repository root as build context, Dockerfile `./Dockerfile`, Free plan, health path `/health`, Auto-Deploy Off, and the variables above. Leave Docker Command empty so the image's startup script runs.

Render's platform-provided `RENDER=true` gates the proxy opt-in. Only on that platform, the application accepts one immediate ingress hop for `X-Forwarded-For` and `X-Forwarded-Proto`, before HTTPS redirection and rate limiting. It does not trust `X-Forwarded-Host`. This relies on Render being the network ingress boundary; do not enable it on an independently exposed container. Outside Render, ASP.NET's default trusted loopback proxies remain in effect.

The API fails early for incomplete Supabase configuration, local-only storage on Render, Render proxy mode outside Render, SMTP-only Render configuration, or no configured hosted sign-in method. No credentials are printed in those errors.

## 5. Migrations and startup

Render Free has no paid pre-deploy command. For this **single-instance testing service**, `scripts/start-container.sh` runs:

```sh
dotnet GetYourGigs.Api.dll --migrate --migrate-only
```

It then `exec`s the normal API. A migration failure stops startup; it never starts against a partially migrated schema. EF applies only pending migrations, including after a cold start; applied migration files are unchanged. No sample data is seeded automatically.

For a future paid deployment, run the migration as one release job with the same image and credentials and set `GYG_MIGRATE_ON_START=false`. The image supports an explicit migration-only invocation:

```sh
docker run --rm --env-file <private-release-env-file> <image> --migrate --migrate-only
```

Back up data before later schema changes. Switching the application back to an old image does not undo schema migrations. Do not run the integration test suite against the hosted application database: it requires a separate administrative PostgreSQL connection and creates/deletes uniquely named `gyg_tests_*` databases.

## 6. Verify before sharing with testers

1. Open `https://<service>.onrender.com/health`: expect 200 and `{"status":"ok"}`. This is process liveness, not a database or provider check.
2. Open `/api/config`: confirm Google configuration, no development email fallback, and no secrets. Open `/api/profiles`: this checks a real database-backed request. Production intentionally does not expose the OpenAPI document.
3. Add the assigned origin to Google and complete actual Google sign-in in the website and on a physical phone.
4. In the mobile sign-in screen or Profile, change the API server to the Render HTTPS origin. This signs the device out of its previous server. The mobile request timeout is now 120 seconds for cold starts; writes are **not automatically replayed** after a timeout. Check the resulting state before repeating an uncertain create/apply/book action.
5. Upload a photo and save its listing. The returned path stays `/uploads/<generated-name>`; reads redirect to the public Supabase object. Redeploy the service, then verify that the photo still loads. The legacy local folder is not used in Supabase mode.
6. With two accounts, verify gig creation, applications, booking, and messages. If email is configured, verify recipient delivery and one-time code use.
7. Repeat one session after the service has slept. The free service can take around a minute to wake. The public URL remains stable; the Mac and local database are no longer needed for hosted operation.

## Verification boundary

Local regression tests cover production code visibility, provider failures and rollback, Supabase request bytes/headers, stable photo redirects, configuration guards, proxy trust behavior, and existing marketplace rules. External HTTP providers are simulated in these tests: real Supabase permissions/TLS, Render ingress, Google consent, and recipient delivery must be checked after credentials and resources exist. Native builds verify compilation, not physical-device behavior. A release publish does not prove the Docker image has been built or launched.

## Official references

- [Render Docker](https://render.com/docs/docker)
- [Render Free limits](https://render.com/docs/free)
- [Render deploy lifecycle](https://render.com/docs/deploys)
- [Render Blueprint reference](https://render.com/docs/blueprint-spec)
- [Render platform variables](https://render.com/docs/environment-variables)
- [Supabase PostgreSQL connections](https://supabase.com/docs/guides/database/connecting-to-postgres)
- [Supabase Data API security](https://supabase.com/docs/guides/api/securing-your-api)
- [Supabase bucket creation](https://supabase.com/docs/guides/storage/buckets/creating-buckets)
- [Supabase server-side storage access](https://supabase.com/docs/guides/storage/security/access-control)
- [Resend email API](https://resend.com/docs/api-reference/emails/send-email)
