# 1. Rewrite the site as a single ASP.NET Core app

Date: 2026-09-06

Status: Accepted

## Context

The AMTARC site ran as a pnpm monorepo: `apps/web` (Next.js 16, App Router) and `apps/api`
(NestJS 11, Prisma + PostgreSQL), deployed as two Docker images behind Traefik. It is live and
working, so a rewrite needs a reason beyond taste.

Two things changed:

1. **The match-booking feature is leaving the codebase.** Match/squad management, the squadding
   algorithm, registrations, the wait-list and the FFTir CSV export are moving to a **separate
   WordPress module** maintained by a third party. The site's "Matchs" entry becomes a link out to
   it. That was the bulk of the application's complexity.
2. **The club wants the remainder maintainable by .NET people.** What is left once matches are
   removed is small: one marketing page, a news module, four editable content sections, and a
   form-based back-office.

Much of the existing architecture exists *because* there are two services in two languages:
a BFF proxy so the browser never holds the admin JWT, cross-service CORS, a cache-bust webhook from
the API to Next's ISR, `API_URL` plumbing, and DTO shapes maintained twice. None of that is
inherent to the problem.

## Decision

Rebuild as **one ASP.NET Core (.NET 10) application**, dropping everything match-related.

- **Razor Pages** renders the HTML for both the public page and `/admin`. Interactivity is a small
  **TypeScript** bundle (`Scripts/site.ts`, built with esbuild) covering the handful of DOM effects
  the design needs — heading width-morph, scroll reveals, count-up, nav-on-scroll, mobile menu.
- **EF Core 10 + Npgsql** against the same PostgreSQL, with EF migrations replacing Prisma's.
- **Tailwind CSS v4** is kept as-is; the existing `globals.css` (CSS-first `@theme` tokens) is
  ported verbatim and compiled by the Tailwind CLI in a build step. No Node in the runtime image.
- **Output caching** with tag eviction replaces Next's ISR plus the revalidate webhook.
- The site remains **1:1** with the current design and French copy — this is a re-platform, not a
  redesign.

## Consequences

**What this removes:** the second service and its Docker image, the BFF proxy and its route
allowlist, cross-service CORS, the revalidate webhook and its shared secret, `API_URL`, and the
duplicated DTO layer. Deployment becomes one container plus PostgreSQL.

**What it costs:** a working system is rewritten, with the risk that carries. Mitigated by keeping
scope at strict parity-minus-matches, porting the design system unchanged, and cutting over only
after a staged parity check (see the roadmap in the README).

**Constraints inherited:** the database schema is kept byte-compatible with the Prisma one
(`text` ids, `timestamp(3) without time zone`, the native `NewsCategory` enum, original constraint
and index names) so production data can be restored into the new schema without transformation.

**Single-instance assumptions** carry over: in-memory rate limiting, in-memory output cache, and
`Database.Migrate()` on startup. Fine for one container; a scaled deployment would need a
distributed cache and a separate migration step.

## Alternatives considered

- **Blazor Web App (static SSR + interactive islands).** Rejected: it adds a render-mode model, an
  interactive runtime, and friction with output caching and CSP — for effects that amount to about
  120 lines of TypeScript.
- **A TypeScript SPA (React/Astro) in front of an ASP.NET Core JSON API.** Rejected: this is the
  current two-tier shape with NestJS swapped for .NET. It keeps the BFF, the CORS, the
  cross-service cache invalidation and the duplicated DTOs — i.e. most of what the consolidation is
  meant to remove.
- **Keep the Node stack and just delete the match modules.** Cheapest option (about a day) and
  genuinely viable, but it does not address the maintainability goal that motivated the move.
- **Razor Pages deprecation risk** was raised. It is not deprecated: it ships on the ASP.NET Core
  LTS train and remains Microsoft's recommendation for page-oriented server-rendered apps. For a
  club site that must survive years of light maintenance, a stable and boring surface is the point.
