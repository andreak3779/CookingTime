# Scope and limits

Precise statements about what this project is, what it is not, and where it stops.

## What this is sized for

A one-page calculator, built as a worked example of modernising a legacy desktop app and of keeping
AI-assisted work checkable. There are no users to support and no uptime to keep. The structure,
tests and gates are larger than a calculator needs, on purpose: the repo shows how the work was done
as much as what it produced.

## What it does not do

- **It is not hosted.** There is no live demo link. You run it locally with `dotnet run`.
- **It is not containerised or deployed.** Phases 9 to 11 (Docker or Podman image, local Kubernetes,
  documentation) are planned in [plan-kubernetes.md](plan-kubernetes.md). Nothing in the repo claims
  they are done.
- **It has no server, database, accounts or user data.** Those absences are decisions with
  conditions for revisiting them ([ADR 0009](decisions/0009-accepted-security-risks.md)).
- **It is not food-safety advice.** The times and temperatures are carried over from the original
  program's tables. The repo does not claim they were checked against food-safety guidance. Use a
  thermometer.
- **It is not a UI framework showcase.** The interface is one form plus help and about pages.

## Where the checks stop

- **Layering is a convention.** The four layers are folders in one project. Review, the phase docs
  and `AGENTS.md` keep the dependency direction, but no tool fails the build if `Domain/` starts
  using `Infrastructure/`. A team would split the layers into projects, which makes the compiler
  enforce the arrows, or add an architecture test.
- **Only unit coverage is gated.** Component-test coverage is about 17 % and integration about 70 %.
  Those tiers exercise Razor and framework code the unit tier does not reach, and their figures are
  informational.
- **The coverage figure measures lines, not quality.** 86 % says the tests execute most of the
  production code. It does not say the assertions are the right ones.
- **The guards are specific.** A guard that checks one shape proves only that shape is absent
  (the history of this in [HOW_THIS_WAS_BUILT.md](HOW_THIS_WAS_BUILT.md)). The personal-data scan
  covers email addresses and home-directory paths, not every possible kind of personal detail.
- **The security review is a review, not a certification.** It covered the OWASP Top 10 (2021)
  against the code as it was on 2026-11-19. It is not a penetration test, and the repo does not
  claim compliance with a standard.

## What a team setting would change

These are suggestions, not decisions already taken.

- Split `Domain`, `Application` and `Infrastructure` into projects so the compiler enforces the
  layering.
- Add a real HTTP layer in front of the static files so security headers (including
  `frame-ancestors`) can be sent, not only a meta CSP.
- Host it and add a smoke test against the deployed copy, after the container work lands.
- Keep as they are: the decision records, the locked restore, the pinned Actions, the coverage gate,
  the guard tests and the phase docs.

## AI-assisted development

The migration was done with an AI coding agent. The tools and models are listed in
[HOW_THIS_WAS_BUILT.md](HOW_THIS_WAS_BUILT.md#tools), along with the defects that got through and how
they were caught. Treat the code as agent-written and human-reviewed, and read the tests and the
decision records before relying on a claim.
