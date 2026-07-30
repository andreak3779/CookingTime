# CookingTime

A cooking-time calculator. Upgrading from a legacy .NET Framework WinForms app to **ASP.NET Core Blazor WebAssembly on .NET 10** with clean architecture, SOLID design, and tests-in-every-phase.

## Status

| Phase | What | Status |
| --- | --- | --- |
| 0 | Docs scaffold | ✅ Done |
| 1 | Blazor host | ✅ Done |
| 2 | Domain layer | ✅ Done |
| 3 | Application + config | ✅ Done |
| 4 | Razor UI | ✅ Done |
| 5 | Integration + feature tests | ✅ Done |
| 6 | Cleanup | ✅ Done |
| 7 | CI gate | ✅ Done |

Full status: [docs/phases/README.md](docs/phases/README.md).

## Documentation

Project documentation lives in [docs/](docs/README.md):

- **Master plan** — [docs/plan.md](docs/plan.md) (locked).
- **Per-phase deliverables** — [docs/phases/](docs/phases/README.md).
- **Architecture decisions** — [docs/decisions/](docs/decisions/README.md).

## Quick start

```bash
dotnet restore
dotnet build -c Release
dotnet test --settings coverlet.runsettings.xml
# Optional: run the coverage gate locally
python3 scripts/check-coverage.py \
  "$(ls -t tests/CookingTime.UnitTests/TestResults/*/coverage.cobertura.xml | head -1)" \
  --threshold 75
dotnet run                           # boots the Blazor app
```

> Phase 5 added a test-only `WebApplication` host under `tests/CookingTime.IntegrationTests/` that
> `WebApplicationFactory<Marker>` uses to spin up an in-process TestServer. The host is **not** a
> production deployment artifact — it lives under `tests/` and is never published.
>
> Phase 7 made the CI gate blocking. Pull requests to `upgrading-to-net-10` and pushes to `phase/*`
> must build, test, and keep `CookingTime.UnitTests` line coverage on production code ≥ 75%. See
> [docs/phases/phase-7-ci.md](docs/phases/phase-7-ci.md) for details and [docs/decisions/0006-coverage-tooling.md](docs/decisions/0006-coverage-tooling.md)
> for the threshold policy.

## Legacy code

The original WinForms source is preserved, untouched, in [Backup/](Backup/) for historical reference. None of the upgraded code references it.

## Contributing

See [docs/plan.md](docs/plan.md) for the upgrade plan and the per-phase docs for what each phase shipped. PRs follow the branch + checkpoint convention in [ADR 0002](docs/decisions/0002-branch-per-phase.md).
