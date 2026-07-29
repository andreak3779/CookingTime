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
| 5 | Integration + feature tests | ⏳ Pending |
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
dotnet run                           # boots the Blazor app (Phase 1 host; placeholder UI)
```

> Phase 4 will swap the placeholder Cooking Time / About pages for the real `EditForm`-backed calculator wired to the Phase 2 domain through the Phase 3 application service. Until then, `/cooking-time` and `/about` show "Coming in Phase N" placeholders.

## Legacy code

The original WinForms source is preserved, untouched, in [Backup/](Backup/) for historical reference. None of the upgraded code references it.

## Contributing

See [docs/plan.md](docs/plan.md) for the upgrade plan and the per-phase docs for what each phase shipped. PRs follow the branch + checkpoint convention in [ADR 0002](docs/decisions/0002-branch-per-phase.md).
