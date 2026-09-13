# Moving the data from the old stack

Runbook for phase 9 of the rewrite: getting the club's news, editable content and uploaded images
out of the Next.js + NestJS stack and into this one.

**The old stack is never touched.** Everything here reads from it; nothing writes to it. Its
database volume (`amtarc_db_data`) and uploads volume (`amtarc_api_uploads`) stay exactly as they
are, which is what makes the rollback in [the cutover plan](#rollback) trivial.

This procedure was rehearsed end to end against a copy of the old database before being written
down; the notes below record what the rehearsal actually found, not what it was expected to find.

## What moves, and what does not

| Table / data | Moves? | Why |
| --- | --- | --- |
| `News` | Yes | The club's real content. |
| `SiteContent` | Yes | Whatever copy an admin has edited. |
| `Admin` | **No** | The new stack seeds its own row from `ADMIN_PASSWORD` on every boot. Generate a fresh password rather than carrying the old hash across. |
| `Match`, `Squad`, `Registration`, `SquadRequest` | **No** | The matches feature is gone — that is the point of the rewrite. |
| `_prisma_migrations` | **No** | Replaced by `__EFMigrationsHistory`, which the migrations bundle writes. |
| Uploaded images | Yes | Volume-to-volume copy, see below. |

## 1. Dump the two tables from the old stack

On the VPS, from the old stack's directory:

```bash
docker compose -f docker-compose.prod.yml exec -T postgres \
  pg_dump -U amtarc -d amtarc --data-only --column-inserts \
    --table='"News"' --table='"SiteContent"' \
  > amtarc-data.sql
```

`--data-only` because the new schema is created by the migrations bundle, not by this dump.
`--column-inserts` so the statements do not depend on column order — the two schemas agree today,
and this keeps the dump working if they ever drift.

Sanity-check before going further:

```bash
grep -c 'INSERT INTO' amtarc-data.sql
```

## 2. Bring up the new stack's database

```bash
docker compose -f docker-compose.prod.yml up -d postgres
docker compose -f docker-compose.prod.yml up -d app
```

The entrypoint runs the migrations bundle, which creates `Admin`, `News`, `SiteContent`, the native
`NewsCategory` enum, and `__EFMigrationsHistory`. Confirm before restoring:

```bash
docker compose -f docker-compose.prod.yml exec -T postgres psql -U amtarc -d amtarc -c '\dt'
```

## 3. Restore

```bash
docker compose -f docker-compose.prod.yml exec -T postgres \
  psql -U amtarc -d amtarc -v ON_ERROR_STOP=1 < amtarc-data.sql
```

`ON_ERROR_STOP=1` matters: without it psql reports failures and carries on, and a partial restore
is worse than none.

### What the rehearsal confirmed

- **The native `NewsCategory` enum survives.** The column stays `USER-DEFINED` / `NewsCategory`,
  not text — this was the main risk, because a provider that mapped the enum to text would have
  produced a schema the dump could not load.
- **`timestamp(3)` values survive to the millisecond.** Hashing `id || slug || category ||
  publishedAt || createdAt` over all rows gives an identical digest either side of the move.
- **The cuid ids come across unchanged**, and sit happily alongside the GUIDs the new app generates
  for new rows. Ids are never user-facing; the slug is the public handle.
- **Newest-first ordering is preserved** on the rendered page.
- **A migrated row is editable in the new back-office**, and editing it leaves the slug alone — so
  any link already shared keeps working — while `updatedAt` moves as it should.

## 4. Copy the uploads

```bash
docker run --rm \
  -v amtarc_api_uploads:/from \
  -v amtarc_uploads:/to \
  alpine cp -a /from/. /to/

# Required. See below.
docker run --rm -v amtarc_uploads:/to alpine chown -R 1654:1654 /to
```

**The `chown` is not optional.** The copy runs as root, which leaves the directory and its contents
owned by root; the app runs as uid 1654 (`app`). Without it, existing images are still served —
so a smoke test looks fine — but *new* uploads fail. The rehearsal hit exactly this. The app now
reports it as a clear error rather than a 500, but the upload still fails until the ownership is
fixed.

Existing `News.imageUrl` values are absolute `https://amtarc.supremjerem.com/uploads/…`. They keep
resolving after the cutover because the domain and the path are unchanged.

## 5. Validate before pointing anything at it

```bash
# Row counts match the source
docker compose -f docker-compose.prod.yml exec -T postgres psql -U amtarc -d amtarc \
  -tAc 'select (select count(*) from "News"), (select count(*) from "SiteContent")'

# The enum is still the native type
docker compose -f docker-compose.prod.yml exec -T postgres psql -U amtarc -d amtarc \
  -tAc "select udt_name from information_schema.columns
        where table_name='News' and column_name='category'"

# Timestamps are byte-identical to the source (run the same query against both)
docker compose -f docker-compose.prod.yml exec -T postgres psql -U amtarc -d amtarc \
  -tAc 'select md5(string_agg(id||slug||category::text||"publishedAt"::text||"createdAt"::text,
                              E'"'"'\n'"'"' order by id)) from "News"'
```

Then in a browser, on the staging host: the news teaser shows the same items in the same order,
their images load, signing in works, editing a migrated item works and does not change its slug,
and uploading a new image works.

## Rollback

Nothing to roll back at this stage — the old stack has not been modified. If the restore goes
wrong, drop the new stack's database volume and start from step 2:

```bash
docker compose -f docker-compose.prod.yml down
docker volume rm amtarc_site_db_data
```
