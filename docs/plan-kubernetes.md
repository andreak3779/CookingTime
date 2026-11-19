# CookingTime Kubernetes Deployment — Plan

> **Status:** Draft — not yet locked. Lock after Phase 9 lands, per the same convention as [`plan.md`](plan.md).

## Goal

Deploy the existing CookingTime Blazor WebAssembly app to a **local Kubernetes cluster**, using tooling already in daily use (Podman CLI, Docker CLI, Podman Desktop). This is a separate initiative from the .NET Framework → .NET 10 migration documented in [`plan.md`](plan.md) — that migration is complete (Phases 0–8, all ✅). This plan does not touch that history or reopen the locked master plan; it continues the same `branch → checkpoint → ADR → docs` workflow under a new version series.

**Why this exists:** Kubernetes has come up repeatedly as a named gap across active job applications (BeyondTrust, Foci Solutions, Aequilibrium, AOT Technologies) despite three completed Pluralsight courses (Kubernetes for Software Development: Foundations, Deploying Your Code, and related). Coursework alone isn't converting into a credible claim — a real deployed project is. This plan is that project.

## Scope boundary

- **Local only.** Target is a local Kubernetes cluster (via Podman Desktop's built-in Kubernetes support, kind-based) — not a cloud-managed cluster (EKS/AKS/GKE). Cloud deployment is a separate, later initiative if pursued.
- **No CI/CD for this plan.** GitHub Actions already blocks on `dotnet test` + coverage (Phase 7 of the migration); this plan doesn't add a cluster to CI. Deployment is manual/local for now.
- **No new application code.** The app itself doesn't change — this plan containerizes and deploys the existing Blazor WASM output, it doesn't add features.

## Final Stack (this plan)

- **Container runtime:** Podman (primary — matches the CLI experience already in use), Docker CLI kept as a compatibility check (`docker build`/`docker run` should work unmodified against the same Dockerfile).
- **Image:** multi-stage Dockerfile — SDK image (`mcr.microsoft.com/dotnet/sdk:10.0`) to `dotnet publish` the Blazor WASM output, then a minimal static-file server image (`nginx:alpine` or equivalent) to serve `wwwroot`/publish output.
- **Cluster:** local, via Podman Desktop's Kubernetes integration (kind-based). `kubectl` for apply/inspect/logs.
- **Manifests:** plain Kubernetes YAML — a `Deployment` and a `Service` (`NodePort` or port-forward for local access). No Helm/Kustomize for this first pass — keep it simple enough to explain in an interview, not impressive-looking for its own sake.

## Branch & Phase Map

Continues the repo's existing phase sequence (this repo's history is 0–8; these are 9–11) under a new tag series (`v1.x`, since `v0.x` was scoped to the .NET 10 migration).

| Phase | Branch | Tag |
| --- | --- | --- |
| 9 — Containerize | `phase/9-containerize` | `v1.0-containerize` |
| 10 — Local Kubernetes deployment | `phase/10-k8s-deploy` | `v1.1-k8s-deploy` |
| 11 — Expose, verify, document | `phase/11-k8s-docs` | `v1.2-k8s-docs` |

Each phase is its own feature branch cut from `main`, ends with one `checkpoint(phase-N): …` commit, tagged for rollback — same discipline as the migration phases.

## Phase summaries

- **Phase 9 — Containerize:** Write the multi-stage `Dockerfile` at repo root. Build the image with `podman build`, verify it also builds with `docker build` (compatibility check, not a preference change). Run the container locally (`podman run`), confirm the app serves and functions correctly in-container before touching Kubernetes at all — isolate container problems from cluster problems. Phase doc: `docs/phases/phase-9-containerize.md`.
- **Phase 10 — Local Kubernetes deployment:** Start Podman Desktop's local Kubernetes cluster. Write `k8s/deployment.yaml` and `k8s/service.yaml`. `kubectl apply -f k8s/`. Verify pod reaches `Running`, check `kubectl logs`, confirm no CrashLoopBackOff. Phase doc: `docs/phases/phase-10-k8s-deploy.md`.
- **Phase 11 — Expose, verify, document:** Reach the app from a browser (port-forward or NodePort), smoke-test the actual cooking-time calculator UI end-to-end running in-cluster. Update `README.md` with a "Kubernetes" section (what's deployed, how to run it, a screenshot). Write the phase doc. This is also the point to update the resume/learning-plan cross-references (`Resume_Snapshot.md`, the master learning plan's Phase 6.0) to reflect this as done rather than planned.

## Decisions

New decisions get recorded as `docs/decisions/0007-*.md` onward as they're actually made during Phase 9–11 work (e.g., why nginx over a .NET static-file server, why NodePort over port-forward) — not written speculatively here. See [`decisions/README.md`](decisions/README.md) for the existing ADR index; this plan continues that same append-only numbering.

## Working agreement

- This plan locks after Phase 9 lands, same convention as `plan.md` — subsequent phase-specific detail goes in `docs/phases/phase-N-*.md`, not back into this file.
- Doesn't reopen or edit `plan.md` — that master plan stays locked and scoped to the .NET 10 migration.
- Same anti-pattern discipline from `AGENTS.md` applies: tests/docs ship with code, not after; ADRs are append-only.

## Run locally (once Phase 9 lands)

```bash
# Build and run the container
podman build -t cookingtime:local .
podman run -p 8080:80 cookingtime:local

# Deploy to local Kubernetes (once Phase 10 lands)
kubectl apply -f k8s/
kubectl get pods
kubectl port-forward svc/cookingtime 8080:80
```

(Actual commands will evolve as phases land — see each phase doc, same as the migration plan.)
