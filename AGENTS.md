# Get Your Gigs

- The API is a single deployable ASP.NET Core monolith. Keep business rules server-side.
- Any account can hire or work. PreferredMode changes context, never authorization or account identity.
- Use the attached original MVP specification in docs/reference/ as historical behavior guidance; README and current contracts describe this implementation.
- Follow the relevant official dotnet/skills guidance (https://github.com/dotnet/skills), especially dotnet-webapi, optimizing-ef-core-queries, maui-data-binding, maui-dependency-injection, and maui-safe-area. The initial implementation consulted commit 96c3d68dad2855e3ab5178aa1a262789d86a1466. Do not assume all marketplace plugins are installed.
- Minimal API endpoint groups call interface-backed services. DTOs are sealed records in Contracts; do not return EF entities.
- Use migrations for PostgreSQL schema changes. Never edit applied migrations.
- Acquire applicant account row locks before gig row locks for application mutations. Keep booking, waitlisting, cancellation, and activity events in the same transaction.
- Application days use Asia/Kolkata. A withdrawal does not refund the daily allowance.
- Never create fake reviews, infer completion from the calendar, or seed unlabeled real-looking activity.
- Native UI belongs in MAUI XAML with compiled bindings, safe-area handling, and view models. Business policy belongs in the API; the shared C# client is transport only.
- Run dotnet test tests/GetYourGigs.Tests/GetYourGigs.Tests.csproj and npm run test --prefix src/GetYourGigs.Web plus npm run build --prefix src/GetYourGigs.Web after relevant changes. Tests require PostgreSQL and create/delete only a uniquely named gyg_tests_* database.
- No automatic commits. Keep credentials, local database files, and build artifacts out of Git.
