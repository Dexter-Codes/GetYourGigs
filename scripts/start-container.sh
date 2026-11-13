#!/bin/sh
set -eu

# Explicit arguments support a separate migration invocation with the same image.
if [ "$#" -gt 0 ]; then
    exec dotnet GetYourGigs.Api.dll "$@"
fi

# Render Free has no pre-deploy job. For this single-instance testing service,
# EF applies pending migrations before listening; a failure prevents startup.
case "${GYG_MIGRATE_ON_START:-false}" in
    true) dotnet GetYourGigs.Api.dll --migrate --migrate-only ;;
    false) ;;
    *) echo 'GYG_MIGRATE_ON_START must be true or false.' >&2; exit 1 ;;
esac

exec dotnet GetYourGigs.Api.dll
