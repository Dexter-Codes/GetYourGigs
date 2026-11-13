#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [ -z "${GYG_TEST_DATABASE:-}" ]; then ./scripts/postgres-start.sh; fi
dotnet test tests/GetYourGigs.Tests/GetYourGigs.Tests.csproj
npm run test --prefix src/GetYourGigs.Web
npm run build --prefix src/GetYourGigs.Web
