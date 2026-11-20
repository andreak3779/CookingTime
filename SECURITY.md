# Security Policy

## Reporting a vulnerability

Please **do not open a public issue** for a security problem.

Report it privately through GitHub:
**[Report a vulnerability](https://github.com/andreak3779/CookingTime/security/advisories/new)**
(the repository's **Security** tab → **Report a vulnerability**).

Include what you found, how to reproduce it, and what you think the impact is.
This is a one-person project, so responses are best-effort: expect an
acknowledgement within about a week, and a fix or a decision on whether it is
in scope as soon as practical after that.

## Supported versions

Only the latest commit on `main` is supported. The project publishes no
packages or release binaries; the phase tags (`v0.x`) are historical
checkpoints and receive no fixes.

## What is in scope

CookingTime is a **client-side Blazor WebAssembly application**. It has no
server-side application code, no database, no API endpoints, no
authentication and no user data. Everything it computes happens in the
visitor's browser from configuration that ships with the app.

In scope:

- A way to run script in a visitor's browser (XSS, an unsafe URL scheme
  reaching a link, and similar).
- A vulnerable dependency that is reachable from the application.
- Weaknesses in the build, CI or release pipeline, for example a workflow
  that could leak a secret or let an outside pull request run privileged code.
- **Personal data in the repository** — a real email address, a personal
  filesystem path or similar in any file, commit, tag or message. This
  project treats that as a security issue
  ([ADR 0008](docs/decisions/0008-no-personal-contact-data.md),
  [ADR 0011](docs/decisions/0011-rewrite-history-into-a-new-repository.md)).

## Known and accepted

Some properties are **absent by design** and are recorded as decisions, not
overlooked. Please read
[ADR 0009](docs/decisions/0009-accepted-security-risks.md) before reporting
any of these:

- No authentication or authorization.
- No cryptography.
- No server-side request surface (SSRF).
- The client configuration file (`wwwroot/appsettings.json`) is public by
  design; it must never contain secrets, and a test enforces that.
- No raw-HTML sink exists.

A change that introduces a server, a database or user-supplied content
invalidates those assumptions, and a report about *that change* is welcome.

## Coordinated disclosure

Please give me reasonable time to fix an issue before disclosing it publicly.
I will credit you in the advisory unless you prefer not to be named.
