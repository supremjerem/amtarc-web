# 2. Cookie authentication for the single admin account

Date: 2026-09-06

Status: Accepted

Supersedes the previous repository's ADR 0001 ("admin auth via httpOnly cookie BFF").

## Context

The back-office has exactly one account. In the Next + NestJS system, authentication was:
NestJS issued an HS256 JWT on `POST /auth/login`; the Next app wrapped it in a BFF — a login route
that stored the JWT in an httpOnly cookie, a catch-all proxy that attached
`Authorization: Bearer …` to every admin call against a per-method route allowlist, and middleware
that did UX-only gating. That design existed to keep the token out of browser JavaScript **across a
service boundary**.

With the rewrite to a single ASP.NET Core app (ADR 0001) there is no boundary left to cross: the
admin pages and the data access run in the same process.

## Decision

Use **ASP.NET Core cookie authentication** directly.

- Cookie `amtarc_admin`: `HttpOnly`, `SameSite=Lax`, `Secure` in production, non-persistent
  (session), with a 7-day sliding server-side ticket — matching the old JWT's 7-day expiry.
- Passwords hashed with **BCrypt.Net-Next** at work factor 10, the same algorithm and cost as the
  previous seed, so the existing hash format is understood.
- The single `Admin` row is **seeded and rotated from configuration on every startup**
  (`Admin:Email` / `Admin:Password`), exactly as the old `prisma/seed.ts` did. Rotating the password
  is: change the value, restart. There is deliberately no change-password screen.
- Authorization is a single `AdminOnly` policy applied to the whole `/Admin` Razor Pages folder,
  with the login and logout pages anonymous.
- Anti-forgery tokens on every admin form (automatic for Razor Pages POST handlers).
- Login is rate-limited separately from the global limiter, and a lockout is reported distinctly
  from bad credentials.

**ASP.NET Core Identity is not used.** It brings user stores, roles, claims management, email
confirmation, 2FA scaffolding and a PBKDF2 hash format — none of which one hard-coded account
needs.

## Consequences

Deleted along with the old design: the JWT signing secret and its 24-character validation,
`passport-jwt`, the BFF login/logout/proxy routes and their allowlist regexes, the Next middleware,
and the `amtarc_admin_token` cookie plumbing. Auth is now the framework's built-in handler plus a
~30-line `AuthService`.

The token is no longer self-contained: sign-out and account changes take effect immediately rather
than at token expiry, which is an improvement. The trade-off is that the session ticket is tied to
this app's data-protection keys — fine for a single instance, but a scaled deployment would need a
shared key ring.

Because the password is re-hashed from configuration on every boot, an operator who loses the value
cannot recover it from the database; they set a new one and restart.
