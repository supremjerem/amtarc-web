#!/bin/sh
#
# Applies pending migrations, then hands the container over to the app.
#
# Migrations run here rather than from the app so a schema change that cannot be applied stops the
# deploy at a clear error, instead of leaving a running app talking to a database it does not
# match. This is the port of the previous stack's `prisma migrate deploy` entrypoint.

set -eu

if [ -z "${ConnectionStrings__Amtarc:-}" ]; then
  echo "entrypoint: ConnectionStrings__Amtarc is not set." >&2
  exit 1
fi

echo "entrypoint: applying database migrations…"
/app/efbundle --connection "$ConnectionStrings__Amtarc"

echo "entrypoint: starting Amtarc.Web…"
# exec so the app becomes PID 1 and receives SIGTERM directly on `docker compose down`.
exec dotnet /app/Amtarc.Web.dll "$@"
