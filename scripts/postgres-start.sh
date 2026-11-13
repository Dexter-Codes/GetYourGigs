#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p .local
if [ ! -f .local/postgres/PG_VERSION ]; then
  initdb -D .local/postgres -U gyg -A trust --no-locale --encoding=UTF8 >/dev/null
fi
if ! pg_ctl -D .local/postgres status >/dev/null 2>&1; then
  pg_ctl -D .local/postgres -l .local/postgres.log -o '-h 127.0.0.1 -p 55432' start
fi
if ! psql -h 127.0.0.1 -p 55432 -U gyg -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='getyourgigs'" | rg -q 1; then
  createdb -h 127.0.0.1 -p 55432 -U gyg getyourgigs
fi
printf 'Local PostgreSQL ready on 127.0.0.1:55432 (loopback only, development trust authentication).\n'
