# Staging, parity check and cutover

Runbook for phase 10: putting this app in front of the club's visitors.

The shape of it: stand the new stack up on a second hostname, check it against the live site item
by item, then move one Traefik router. The old stack keeps running throughout, so backing out is
a matter of moving that router back.

## Prerequisites (need the maintainer, not Claude)

1. A DNS `A` record for `amtarc-next.supremjerem.com` pointing at the VPS.
2. `~/apps/live/amtarc-site/` on the VPS containing `docker-compose.prod.yml` and a filled-in
   `.env` — copied from `.env.prod.example` **to `.env`, not `.env.prod`**. `docker compose` only
   auto-loads `.env`; a stack started without one boots with a blank environment and crash-loops,
   which is how the old stack once went down.
3. Fresh secrets, not the old stack's: `openssl rand -base64 32` for `POSTGRES_PASSWORD` and for
   `ADMIN_PASSWORD`.

## 1. Staging

Set `DOMAIN=amtarc-next.supremjerem.com` in `.env`, then:

```bash
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
```

Traefik picks up the router from the labels and requests a certificate. Watch it come up:

```bash
docker compose -f docker-compose.prod.yml logs -f app
curl -sI https://amtarc-next.supremjerem.com/healthz
```

Then follow [the data migration runbook](data-migration.md) to load the real content.

## 2. Parity checklist

Against `https://amtarc.supremjerem.com` (live) and `https://amtarc-next.supremjerem.com` (new),
side by side:

**Content**
- [ ] All eight sections present, in the same order, with identical copy
- [ ] Nav has no "Matchs" entry — that feature is gone, and the link out to the WordPress module
      is a separate follow-up
- [ ] News teaser shows the same published items, newest first, with their images
- [ ] Footer year is current

**Design**
- [ ] The three type roles render (Archivo display, Instrument Sans body, Martian Mono data)
- [ ] The heading width-morph fires on scroll — this needs the `wdth` axis, and its absence is
      silent
- [ ] Scroll reveals, the stat count-up, the nav-on-scroll state, the mobile menu
- [ ] With "reduce motion" on: fades survive, movement and counting do not
- [ ] Honeycomb texture and the hero gradient look right

**Back-office**
- [ ] Sign in with the new credential
- [ ] Create, edit and delete a news item; confirm each shows on the public page within seconds
- [ ] Edit each of the four content sections, and reset one back to defaults
- [ ] Upload a JPEG, PNG, WebP and GIF; confirm an SVG and a PDF are refused
- [ ] Editing a migrated item does not change its slug

**Operations**
- [ ] `curl -I` shows the security headers, and no `Access-Control-Allow-Origin`
- [ ] HTTPS with a valid certificate; HSTS present
- [ ] Hammering `/admin/login` produces the "trop de tentatives" message, distinct from a wrong
      password
- [ ] `/healthz` returns 200; the container reports healthy
- [ ] Restart the container and confirm the admin session survives — that is the data-protection
      key ring being persisted
- [ ] Lighthouse, compared against the live site

## 3. Cutover

Pick a quiet moment; the whole thing is a couple of minutes.

1. **Freeze admin edits** on the old site — anything written after the final dump is lost.

2. **Final delta** — repeat [steps 1, 3 and 4 of the data migration](data-migration.md), into the
   already-running new database. Truncate first so the restore is not fighting the earlier copy:

   ```bash
   docker compose -f docker-compose.prod.yml exec -T postgres \
     psql -U amtarc -d amtarc -c 'truncate "News", "SiteContent"'
   ```

   Then restore the fresh dump and re-run the uploads copy **and its `chown`**.

3. **Move the router.** In the old stack's compose file, remove (or comment out) the
   `traefik.http.routers.amtarc.rule` label; in the new stack's `.env`, set
   `DOMAIN=amtarc.supremjerem.com`. Then:

   ```bash
   # old stack: stop answering for the live host
   cd ~/apps/live/amtarc && docker compose -f docker-compose.prod.yml up -d

   # new stack: start answering for it
   cd ~/apps/live/amtarc-site && docker compose -f docker-compose.prod.yml up -d
   ```

   Two routers must never claim the same `Host()` at once — Traefik will pick one and it may not
   be the one you meant.

4. **Verify** the operations part of the checklist above, now on the live hostname.

## Rollback

The old stack is still running with its data intact. Undo step 3:

- restore the `Host(amtarc.supremjerem.com)` label on the old `web` service,
- remove it from the new stack (or set `DOMAIN` back to `amtarc-next`),
- `docker compose up -d` on both.

Anything an admin wrote in the new back-office after the cutover would need re-entering by hand;
nothing else is lost. Keep the old images, compose file and volumes for **at least two weeks**.

## Afterwards

- [ ] Point the "Matchs" nav entry at the WordPress module once it exists
- [ ] Refresh `docs/screenshot.jpg` from the live site
- [ ] Note in the old repo's README that it is archived and superseded
- [ ] Remove the two `[TEST]` matches from the old stack before retiring it
