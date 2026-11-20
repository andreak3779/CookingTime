# How this was built

## The short version

CookingTime started as a single-file .NET Framework 2.0 WinForms calculator (about 700 lines in
`ComCookingTime.cs`, kept under `OriginalSource/`). It was rebuilt as a layered .NET 10 Blazor
WebAssembly app over 19 shipped phases, using an AI coding agent. The durable part is not the
calculator. It is the workflow that kept the agent's output checkable: a written plan, one branch
and one checkpoint commit per phase, a decision record for each non-obvious choice, standing rules
in `AGENTS.md`, and a CI gate that fails the build instead of trusting anyone's say-so.

## Tools

- **VS Code** with **GitHub Copilot** as the coding assistant.
- **Ollama** to run the models: **MiniMax M3**, both the full model (`minimax-m3:cloud`) and the
  Flash model.
- **Git and GitHub** for branches, pull requests, Actions and Dependabot.
- **Local checks** the agent's output has to pass: `dotnet build` with warnings as errors,
  `dotnet test` across three test projects, coverlet and a coverage-threshold script.

## The process

1. **A locked plan.** `docs/plan.md` was written before the first phase and then locked
   ([ADR 0003](decisions/0003-docs-as-code.md)). It changes only through later phase docs, such as
   the [status note](phases/master-plan-status-note.md), never by editing the plan.
2. **One branch per phase.** Each phase ends in exactly one `checkpoint(phase-N): …` commit and a tag,
   so a bad phase is one `git reset --hard` away from the last good one
   ([ADR 0002](decisions/0002-branch-per-phase.md)).
3. **A phase doc for each phase.** It says what shipped, what was verified and with which numbers
   ([phases/](phases/README.md)).
4. **Decision records.** Choices that are not obvious became a short ADR, appended and never
   overwritten ([decisions/](decisions/README.md)). There are eleven.
5. **Standing rules for the agent.** [AGENTS.md](../AGENTS.md) holds the layering rules, naming,
   anti-patterns and the reading order, so the rules live in the repo and not in a chat session.
6. **Tests ship with the code.** Each phase lands with its unit tests and, where it applies,
   component (bUnit) and integration (`WebApplicationFactory`) tests.
7. **CI is the definition of done.** A phase merges only when the gate is green
   ([architecture.md](architecture.md#ci-and-the-build-gate)).

## How the checks constrain the agent

- **`TreatWarningsAsErrors`.** A green build is a real green build.
- **The coverage gate.** Unit-test line coverage on production code must stay at or above 75 %
  (86 % when measured on 2026-11-20).
- **Locked restore.** `--locked-mode` plus committed lock files means a dependency change has to be
  made on purpose and shows up as a reviewable diff.
- **Guard tests** pin properties that are easy to break quietly: where the config file lives, one
  component per route, no test assemblies in the publish output, no personal data in tracked files.
- **The agent's rules** (`AGENTS.md`) name the anti-patterns that already happened once, with the
  phase that fixed each, so they are not reintroduced.

## Where the build went wrong, and how it was caught

Every row below was a real defect in agent-written code or in the repo's own checks. The point of
the table is that none of them was caught by a green build.

| What went wrong | How it was caught | What now prevents it |
| --- | --- | --- |
| **The app did not boot.** `index.html` referenced `blazor.web.js`, which a standalone WebAssembly project does not emit. Build and all 118 tests were green; the page 404'd in the browser (Phase 7b). | Running the app and reading the browser console. | Integration tests that assert the boot script reference and that the file is served. |
| **Two components claimed the same routes.** A design package copied into the repo was compiled in by a default Razor glob, so the router threw "ambiguous routes" in the browser only (Phase 12, [ADR 0007](decisions/0007-design-package-as-reference-artifact.md)). | Running the app. Build, 125 tests and `GET /` were all green. | An explicit compile glob and a test over the client route table. |
| **The browser received no configuration.** `appsettings.json` sat at the repo root, but the WebAssembly SDK bundles only `wwwroot/appsettings*.json`, so the meal catalog was empty (introduced in Phase 3, found and fixed in Phase 13). | A full-codebase review that probed the running app. | The config moved under `wwwroot/`, and a guard test pins the location. |
| **Meals ignored their own weight range.** A 500 lb pork roast produced an inverted cook range and the UI showed it as a success (introduced in Phase 3, fixed in Phase 14). | The same review, with probes against the compiled assemblies. | `RangeMeal` enforces its range, durations reject inverted bounds, and end-to-end tests use the real meal type instead of a mock. |
| **Reset left stale validation messages.** The form's state is keyed to the model instance, so changing the model in place never cleared them (introduced in Phase 4, fixed in Phase 17). | The review. | `Reset()` replaces the view model, and a bUnit test submits, resets and asserts the messages clear. |
| **Personal data survived two audits.** A binary IDE file and legacy executables held data as UTF-16 text that a text search could not see, and a config guard checked only for `@` (Phases 13, 15, 19, 20). | A byte-level sweep that looked for the data in both encodings. | A guard that scans every publishable file, binaries included, and a history rewrite published as a new repository ([ADR 0010](decisions/0010-redact-pii-in-original-source.md), [0011](decisions/0011-rewrite-history-into-a-new-repository.md)). |

Two lessons the repo records in its own words:

- **A green build is not a working app.** The first two rows passed every automated check. The
  fix was to test the thing that actually failed, in the layer where it failed.
- **A guard that checks one shape proves only that shape is absent.** A check for `@` says nothing
  about a token that has no `@`.

## The review that followed

After the migration, a full-codebase review read every production file, probed the compiled
assemblies and the running app, and ran an OWASP Top 10 (2021) pass. It found no critical or high
exploitable vulnerability. It did find the correctness and trust-boundary problems above. Each
finding was fixed in Phases 14 to 17 or recorded as an accepted risk with the condition that would
reopen it ([plan-codebase-review.md](plan-codebase-review.md),
[ADR 0009](decisions/0009-accepted-security-risks.md)). Personal data was checked separately and
removed ([plan-remove-pii.md](plan-remove-pii.md)).

## Reading order

1. [README](../README.md): what it does and the phase table.
2. [architecture.md](architecture.md): the layers, the domain, the diagrams.
3. [decisions/](decisions/README.md): why, one decision at a time.
4. [quality-attributes.md](quality-attributes.md) and [SCOPE-AND-LIMITS.md](SCOPE-AND-LIMITS.md):
   what it optimises for and where it stops.
5. [onboarding.md](onboarding.md): a guided code tour.
