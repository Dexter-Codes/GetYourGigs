#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [ -z "${ConnectionStrings__Database:-}" ]; then ./scripts/postgres-start.sh; fi
python3 scripts/configure-local.py
dotnet tool restore
dotnet restore src/GetYourGigs.Api
npm ci --prefix src/GetYourGigs.Web
npm run build --prefix src/GetYourGigs.Web
exec dotnet run --project src/GetYourGigs.Api -- --seed
