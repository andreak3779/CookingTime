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
| 6 | Cleanup | ⏳ Pending |
| 7 | CI gate | ⏳ Pending |

Full status: [docs/phases/README.md](docs/phases/README.md).

## Documentation

Project documentation lives in [docs/](docs/README.md):

- **Master plan** — [docs/plan.md](docs/plan.md) (locked).
- **Per-phase deliverables** — [docs/phases/](docs/phases/README.md).
- **Architecture decisions** — [docs/decisions/](docs/decisions/README.md).

## Quick start (will evolve as phases land)

```bash
dotnet restore
dotnet build -c Release
dotnet test                          # runs every test project that exists
dotnet run                           # boots the Blazor app
```

> Phase 5 added a test-only `WebApplication` host under `tests/CookingTime.IntegrationTests/` that
> `WebApplicationFactory<Marker>` uses to spin up an in-process TestServer. The host is **not** a
> production deployment artifact — it lives under `tests/` and is never published.

## Legacy code

The original WinForms source is preserved, untouched, in [Backup/](Backup/) for historical reference. None of the upgraded code references it.

## Contributing

See [docs/plan.md](docs/plan.md) for the upgrade plan and the per-phase docs for what each phase shipped. PRs follow the branch + checkpoint convention in [ADR 0002](docs/decisions/0002-branch-per-phase.md).
