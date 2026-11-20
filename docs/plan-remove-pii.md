# PII analysis and removal plan — CookingTime

> **Status:** ✅ Approved and implemented in Phase 13 (branch
> `phase/13-remove-email-setting`). The decision is recorded as
> [ADR 0008](decisions/0008-no-personal-contact-data.md); the per-phase truth
> is [phase-13-remove-email-setting.md](phases/phase-13-remove-email-setting.md).
>
> **Supersedes** the untracked working-tree draft `docs/plan-remove-email-setting.md`
> (several of its facts were verified wrong; it was deleted in Step 0).
>
> **Redaction convention:** the personal Gmail address that lived at
> `appsettings.json` under `ProductInfo:Email` is referred to below as
> `ADDRESS`. It is deliberately not repeated in this document, so that this file can
> be committed without re-publishing the personal data it plans to remove.
>
> **Correction applied during implementation (Phase 13):** §2 originally claimed
> the address "ships to every browser" because the WASM host fetches
> `appsettings.json` at boot. That was **wrong for this repo** — the SDK only
> bundles `wwwroot/appsettings*.json`, and the file lived at the repo root, so
> the browser received **no configuration at all** (a functional bug: the
> production meal catalog was empty). Phase 13 therefore *also* moves the file
> into `wwwroot/`, which makes it served — and makes this removal essential
> rather than cosmetic.

---

## 1. PII inventory — verified

A full sweep of the working tree and every git ref (`git grep` across all
branches, tags, and remote-tracking refs; plus `git log -S` probes) found five
distinct email-shaped strings. Ground truth per string:

| # | String | Where it actually lives | Published to the public repo? | Disposition |
| --- | --- | --- | --- | --- |
| 1 | `ADDRESS` (owner's personal Gmail) | `appsettings.json` under `ProductInfo:Email` — the only *tracked* occurrence; 6 more in `bin/`/`obj/` build copies (gitignored, gone after a rebuild) | **Yes** — since 2026-07-29 | **Remove end-to-end (§5)** |
| 2 | `Andrea K` / `andrea` / `Andrea K.` (given name) | Git **author *and* committer identity** on every commit: 25/25 commits on `origin/main`, same on `upgrading-to-net-10`. Also tagger on 7 local annotated tags (`v0.0`…`v0.2`, `v0.3`–`v0.8`) | **Yes** — every published commit | **Decision (§7, B)** — switch to noreply going forward; rewrite optional |
| 3 | `/home/<user>/...` (OS username, local paths) | [AGENTS.md:3](../AGENTS.md), [docs/LINKEDIN.md:4,29](../docs/LINKEDIN.md), [docs/phases/phase-0-docs-scaffold.md:71](../docs/phases/phase-0-docs-scaffold.md) | **Yes** — all three are on `origin/main` | **Fixed in Phase 13** — trivial doc edits |
| 4 | `THIRD-PARTY-ADDRESS` (third party — the legacy WinForms author) + a personal homepage URL on the same ISP domain | [OriginalSource/frmAbout.cs:40,42](../OriginalSource/frmAbout.cs) | **Yes** — on `origin/main` (via `446989d`, 2025-08-03) | **Redacted in Phase 19** — see [ADR 0010](decisions/0010-redact-pii-in-original-source.md) |
| 5 | `dev@example.com` | Test fixture ([AboutRazorTests.cs](../tests/CookingTime.ComponentTests/AboutRazorTests.cs)) | Yes | **Keep** — RFC 2606 reserved example domain, not a real address |

Non-findings (verified clean): `wwwroot/`, `Layout/`, `Program.cs`, CI workflow,
`scripts/check-coverage.py`, all `.resx` files, `docs/diagrams/`, `tests/*/bin`.
No secrets, tokens, phones, or street addresses anywhere tracked.
`.github/CODEOWNERS` uses the GitHub handle `@andreak3779` — that is a repo-ops
reference, not contact PII, and stays.

> **Corrected in Phase 19:** `CookingTime.suo` was originally listed here as a
> non-finding on the strength of a binary `strings` sweep. That sweep was
> **filtered on the email pattern**, and the file contains no address — it
> contains a Windows user-profile path (the original developer's home
> directory). An unfiltered sweep found it, and the file was deleted. See
> §7-C and [ADR 0010](decisions/0010-redact-pii-in-original-source.md).

**Corrected during implementation:** an earlier draft of this plan listed the
CI-published coverage XML as a PII surface, because a *local* run embeds
`<source>/home/<user>/src/CookingTime/</source>`. That is a **false positive**:
on a GitHub runner the source root is `/home/runner/work/<repo>/<repo>/`
(verified against the real CI log for run `36658844870`, which uploaded artifact
`coverage-cobertura`), which carries no owner data. The local XMLs are gitignored
and never published. No fix is required.

---

## 2. How the personal address is actually exposed

Four independent surfaces, in increasing order of difficulty:

1. **Working tree config** — `appsettings.json` under `ProductInfo:Email`. One
   line. Trivially removable.
2. **Rendered app output** — `Pages/About.razor` renders a `mailto:` link from
   `ProductInfo.Email`. Any deployed instance publishes the address in HTML —
   *provided the client has a populated `ProductInfoOptions`*. Before Phase 13
   it did not (see #3), so in practice this link never rendered.
3. **Browser-delivered config — the important correction.** The Blazor WASM
   SDK only bundles configuration that lives in `wwwroot/`:

   ```xml
   <!-- Microsoft.NET.Sdk.BlazorWebAssembly.5_0.targets:132 -->
   <_BlazorConfigFile Include="wwwroot\appsettings*.json" />
   ```

   The repository kept `appsettings.json` at its **root**, and `git log
   --diff-filter=A -- wwwroot/appsettings.json` shows it was **never** in
   `wwwroot/`. Proven by publishing three probe apps and extracting the embedded
   boot manifest from `dotnet.js`:

   | Probe | Config location | Boot manifest `appsettings` key |
   | --- | --- | --- |
   | A/B | repo root **and** `wwwroot/` | `['../appsettings.json']` ✅ |
   | Control | repo root only | **ABSENT** ❌ |
   | CookingTime (before Phase 13) | repo root only | **ABSENT** ❌ |

   **Two consequences.** First, the browser received **no configuration at
   all**: `IOptions<ProductInfoOptions>` fell back to POCO defaults *and*
   `MealCatalogOptions.Catalog` was empty, so the production meal dropdown
   offered only the two code-baked meals (Chicken, Turkey) — a functional bug
   no test caught, because bUnit mocks `IMealFactory` and the integration
   suite loads the file **server-side** from the content root. Second, the PII
   exposure was therefore *latent*, not live: moving the file into `wwwroot/`
   is the fix for the bug, and it simultaneously makes the file **served to
   every visitor** — which is exactly why removing the address must land in the
   same phase.
4. **Git history of a public repo** — introduced by `d574e18`
   (2026-07-29, `checkpoint(phase-3)`); it is in the current tree of all five
   published refs (`origin/main`, `upgrading-to-net-10`, `phase/8-rename-original-source`,
   `docs/diagrams-and-pitch-artifacts`, `fix/meal-registry-range-overwrite`) and of
   seven local tags. `git show d574e18:appsettings.json` still returns line 103.
   Removing it here requires a history rewrite — gated, see §7.

**Honest framing on #4:** the public repo URL is
`github.com/andreak3779/CookingTime` — the address's *local part is identical to the
publicly published GitHub handle*, so a human who wants the address can reconstruct
it trivially. The value of scrubbing is therefore not secrecy against determined
humans; it is (a) stopping the app from *publishing* the address, and (b) reducing
exposure to automated scrapers that feed on literal `@gmail.com` strings in source.
The address should be treated as **already public for ~4 months**.

---

## 3. Ground-truth corrections vs the superseded draft

The untracked draft `docs/plan-remove-email-setting.md` was written before full
verification. Corrections (all empirically re-verified):

| Claim in draft | Verified reality |
| --- | --- |
| The email appears in **4 commits** | **6** touches total, but **5 are local-only agent-checkpoint refs** (`agents/…`, never pushed). On real branches it was introduced **once** (`d574e18`) and never removed — so a single `--replace-text` blob rewrite covers all real-branch history |
| "a placeholder address lives in `OriginalSource/`" | **False** — searched every commit ever made (`-S` across `--all`, `--pickaxe-all`): the string has only ever existed inside the draft plan doc itself. `OriginalSource/` is clean of it |
| Deleting `BinderProbe.cs` was blocked by the permission system | **No longer true** — sandbox now allows `rm` inside the repo (verified). Step 0 can complete normally |
| `OriginalSource/` carries 4 email-shaped strings | Carries **2** (a third-party address and a homepage URL on the same domain) in `frmAbout.cs`, plus `AssemblyCompany("Andrea")` in [OriginalSource/AssemblyInfo.cs:12](../OriginalSource/AssemblyInfo.cs) — a bare first name. **All three redacted in Phase 19** — see [ADR 0010](decisions/0010-redact-pii-in-original-source.md) |
| Test totals: "152 facts / 151 after / +1 guard" | Keep the **relative** math: −1 (BinderProbe deleted) +1 (new guard) = **net 0**. Absolute totals change as other phases land; do not hard-code them |

Also factual: [docs/LINKEDIN.md:29](../docs/LINKEDIN.md) calls the source repo
"private" — it is **public** (`gh repo view` → `PUBLIC`, 0 forks, 1 watcher, 8 merged
PRs). Fix while editing docs (§6).

---

## 4. Decisions required (summary)

| # | Decision | Options | Recommendation |
| --- | --- | --- | --- |
| A | Remove `ProductInfo.Email` end-to-end (property + UI + config + tests) vs config-line-only | A: full removal / B: JSON line only | **A — full removal.** The request is to remove the email; a dormant code path that can still render it does not achieve that (same reasoning as the superseded draft, retained) |
| B | Purge address + name/email identity from **git history** (force-push, ref-tag rewrite) | Rewrite vs accept-and-go-noreply | **Accept + go noreply.** Low benefit (§2 framing), real cost: rewrites all commit SHAs from mid-2026 onward, invalidates the 8 existing PR references, requires collaborator re-clones. If wanted anyway, the exact procedure is §7-B — do it as its own task |
| C | Third-party address in `OriginalSource/frmAbout.cs` | Leave verbatim / remove `OriginalSource/` via ADR | **Redacted in place (Phase 19)** — the owner approved removing or emptying the PII. The third-party address and homepage URL are now empty strings, and the binary `.suo` (which carried a Windows user-profile path) is deleted. Recorded in [ADR 0010](decisions/0010-redact-pii-in-original-source.md), which supersedes ADR 0008 decision 7 |
| D | **Move `appsettings.json` into `wwwroot/`** so the WASM client actually receives configuration | Move / leave broken | **Move — owner decision, 2026-11-19.** Fixes the functional bug in §2 #3 (empty production meal catalog) and makes the config delivery path explicit. Consequence: the file becomes served to every visitor, so decisions A and D must land together |
| E | Container document root for Phase 9 | `wwwroot/` only / publish root | **`wwwroot/` only.** The publish root contains `appsettings.json`; serving it would re-expose configuration to every visitor. Pinned in ADR 0008 decision 3 |

Decisions A, C, and D are for the requester. Steps §5 and §6 proceed on A + D.
**Accepted: A, D, E.** B: accept-and-go-noreply. C: leave verbatim.

---

## 5. Phase 13 — remove the Email product-info setting end-to-end (Option A)

New branch per [ADR 0002](decisions/0002-branch-per-phase.md):
`phase/13-remove-email-setting`. **Sequencing note:** ship this *before* Phase 9
(containerize) so the container build never bakes the address into a served file.

### Step 0 — delete planning leftovers (now unblocked)

```bash
rm tests/CookingTime.UnitTests/Application/BinderProbe.cs   # planning probe, verified-passing, not production
rm docs/plan-remove-email-setting.md                        # superseded draft, carries ADDRESS 6×
```

Both are **untracked**; nothing to `git rm`.

### Step 1 — move `appsettings.json` into `wwwroot/` **and** drop the `Email` line

Move the file (`git mv appsettings.json wwwroot/appsettings.json`) so the Blazor
WASM SDK bundles it into the client boot manifest — this is the fix for the
functional bug in §2 #3 (the browser received no configuration, so the meal
catalog was empty in production). Then remove the `"Email"` line; also drop the
trailing comma on the `"ResumeUrl"` line above it or the JSON breaks:

```diff
     "ResumeUrl": "https://github.com/andreak3779/CookingTime",
-    "Email": "ADDRESS"
+    "ResumeUrl": "https://github.com/andreak3779/CookingTime"...  (no trailing comma)
```

(Exact final JSON: `"ResumeUrl"` becomes the last key of `ProductInfo`.)

### Step 2 — [Infrastructure/Configuration/ProductInfoOptions.cs](../Infrastructure/Configuration/ProductInfoOptions.cs)

Delete `public string Email { get; set; } = "";`.

The binder silently ignores unknown config keys, so any deployed
`appsettings.json` still carrying `ProductInfo:Email` keeps working — no startup
exception. Record this in the phase doc.

### Step 3 — [Pages/About.razor](../Pages/About.razor)

Remove the conditional `mailto:` block (lines 20–23). The `ResumeUrl` block above
it stays. `ProductInfoOptions` remains consumed only here, so no other page is
affected (verified: `grep -rn ProductInfo` across `Pages/`, `Layout/`).

**Also remove the mirrored copy** in
[docs/design/cookingtime-theme/razor/Pages/About.razor](../docs/design/cookingtime-theme/razor/Pages/About.razor).
It is a tracked reference artifact ([ADR 0007](decisions/0007-design-package-as-reference-artifact.md)),
not compiled — but the app is updated by copying files out of it, so leaving the
block there means the next theme re-apply reintroduces it **and breaks the
build** (the property no longer exists).

### Step 4 — [tests/CookingTime.ComponentTests/AboutRazorTests.cs](../tests/CookingTime.ComponentTests/AboutRazorTests.cs)

*Must land in the same commit as Steps 2–3* — without the property, the fixture
lines `o.Email = …` stop compiling.

- `RendersProductInfo_FromOptions`: delete `o.Email = "dev@example.com";` and the
  `markup.Should().Contain("dev@example.com")` assertion.
- `RendersDefaults_WhenOptionsNotConfigured`: delete `o.Email = "";`. The existing
  `NotContain("mailto:")` assertion becomes **vacuous** once the block is gone, so
  replace it with a regression guard that has teeth:

```csharp
[Fact]
public void ProductInfoOptions_HasNoEmailProperty()
{
    // The Email option was removed in Phase 13 so the app stops publishing
    // a mailto: address. Guard against it being reintroduced.
    typeof(ProductInfoOptions)
        .GetProperties()
        .Should().NotContain(p => p.Name == "Email");
}
```

Net test delta: −1 (BinderProbe) +1 (guard) = 0.

### Step 5 — docs

| File | Change |
| --- | --- |
| `docs/decisions/0008-no-personal-contact-data.md` | New ADR — the standing decision (see [ADR 0008](decisions/0008-no-personal-contact-data.md)) |
| `docs/decisions/README.md` | ADR index row |
| `docs/phases/phase-13-remove-email-setting.md` | New phase doc — the per-phase truth (from ADR 0003) |
| `docs/phases/README.md` | Phase 13 row + per-phase file link |
| `docs/plan.md` | **Locked — no change** (verified: contains no email reference) |
| `docs/phases/phase-3-*`, `docs/phases/phase-4-*` | **Historical — no change** (per-phase truth is append-only) |
| `docs/onboarding.md` | No change needed (verified: describes the file's role, does not enumerate fields) |
| `README.md` | No change (verified: no email mention) |

### Step 6 — verify + checkpoint commit

```bash
dotnet build -c Release          # 0 warnings (TreatWarningsAsErrors), 0 errors
dotnet test  -c Release          # all green, count unchanged (−1 +1)
```

Then:

- `grep -rn ADDRESS` over the working tree → no tracked-file hits
  (`bin/`/`obj/` copies refresh on next build; gitignored).
- `wwwroot/appsettings.json` still parses (note: it carries a leading
  `// filepath:` comment, so strict `json.loads` **fails** — the .NET config
  provider tolerates comments; verify with the provider, not a strict parser).
- `/about` renders with no `mailto:` and no regression.
- **Config delivery verified**: publish and extract the boot manifest — the
  `appsettings` key must be present (it was `ABSENT` before the move).
- Checkpoint commit message per the phase convention, then tag only after the
  identity hygiene in §6 so new tags don't carry the old email identity.

---

## 6. Hygiene going forward (small, separate commit on the same branch)

1. **Switch the repo identity to GitHub noreply** before any further commits —
   verified numeric-ID form (GitHub API `id: 11012375`):

   ```bash
   git config user.name  "andreak3779"
   git config user.email "andreak3779+11012375@users.noreply.github.com"
   ```

   This stops *new* history from adding more Gmail-stamped commits. It does not
   rewrite anything existing (that is §7-B's whole point).

   **Sandbox note:** `.git/config` is read-only in the agent environment
   (`error: could not write config file .git/config: Device or resource busy`),
   so the agent applies the identity per-invocation instead:

   ```bash
   git -c user.name="andreak3779" \
       -c user.email="andreak3779+11012375@users.noreply.github.com" commit -m "..."
   ```

   The **owner** applies the repo-level setting once, locally.
2. **Scrub local filesystem paths from tracked docs** (§1 item 3):
   - [AGENTS.md:3](../AGENTS.md): `"/home/<user>/src/CookingTime"` → `"this repo"`.
   - [docs/LINKEDIN.md:4,29](../docs/LINKEDIN.md): replace the absolute local path
     with the public repository URL; while there, correct the "private" claim to
     "public" (§3).
   - [docs/phases/phase-0-docs-scaffold.md:71](../docs/phases/phase-0-docs-scaffold.md):
     the skill path → `~/.agents/skills/`.
3. **Optionally** add a scan step to `.github/workflows/ci.yml` that greps for
   `ADDRESS`-shaped Gmail strings in tracked files, so a stray revert cannot
   reintroduce one silently. One grep job, no new tooling.

---

## 7. Decision-gated follow-ups (separate tasks, only if approved)

### B — History rewrite (not recommended by default)

Goal: make `git show d574e18:appsettings.json` (and any earlier commit's tree)
stop containing `ADDRESS`, and stamp all historical commits with the noreply
identity. Verified blast radius: **0 forks** (`gh api … --jq .forks_count` → 0),
8 merged PRs, solo author across both real branches — the cheapest rewrite
scenario there is. Still destructive to commit SHAs.

Verified procedure (run on a fresh clone, with `ADDRESS` replaced by the real
string):

```bash
pipx install git-filter-repo
git clone --no-local . ../CookingTime-rewrite   # fresh clone: filter-repo refuses local dirs
cd ../CookingTime-rewrite

cat > /tmp/replacements.txt <<'EOF'
ADDRESS==>REDACTED
<third-party-address>==>REDACTED        # see note below
<third-party-homepage-url>==>REDACTED
EOF

# The two third-party strings are NOT written out in this document - a doc
# that quotes the address it plans to remove republishes it. Recover them
# from the historical blob instead, then paste them into the placeholders:
#
#   git show 446989d:Backup/frmAbout.cs
#
# Phase 19 already removed them from the working tree, so the only place
# they survive is the history this procedure scrubs.

git filter-repo \
  --replace-text /tmp/replacements.txt \
  --email-callback '
    return commit.emails[0] == b"ADDRESS" and b"andreak3779+11012375@users.noreply.github.com" or commit.emails[0]
  ' \
  --sensitive-data-removal

# force-push everything (branches + tags)
git push --force --all origin
git push --force --tags origin
```

Caveats recorded so nobody is surprised:

- Every commit SHA from mid-2026 onward changes; the 8 existing PR numbers keep
  their numbers but their SHAs become stale; local clones and the local
  `agents/…` checkpoint refs must be discarded or re-cloned.
- Local annotated tags `v0.0`–`v0.2`, `v0.3`–`v0.8` carry the Gmail tagger
  identity; only `v0.7b-blazor-boot-script` is currently on origin (verified via
  `git ls-remote --tags`). Pushing tags *after* a rewrite is the natural point to
  drop or re-create them.
- **GitHub-side caches** (old commit views, raw URLs, forks if they ever appear)
  are not scrubbed by pushing; GitHub Support can run a
  [sensitive-data removal](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository)
  for the two published branches. Given the §2 framing, this is also fine to skip.
- After a rewrite, verify: `git show d574e18:appsettings.json` no longer exists
  under that SHA (reflog/note the pre-rewrite SHA first), and
  `git log --all --format='%ae' | sort -u` shows only the noreply address.

### C — Third-party PII in `OriginalSource/`

Repo rules (AGENTS.md, [Phase 8 doc](phases/phase-8-rename-original-source.md))
required `OriginalSource/` to stay verbatim, and editing `frmAbout.cs` to redact
the third-party address would have violated that rule.

**Resolved in Phase 19.** The owner approved removing the PII, so the folder no
longer needs to be untouched: the third-party address and homepage URL are now
empty strings, the `AssemblyCompany` first name is empty, and the binary
`.suo` IDE artifact — which carried a Windows user-profile path that no text
sweep had caught — is deleted. The rule change is recorded in
[ADR 0010](decisions/0010-redact-pii-in-original-source.md), which supersedes
ADR 0008 decision 7 and ADR 0009 §6.

> **Correction to this plan's earlier sweep.** §1 listed `CookingTime.suo` as a
> "non-finding" on the strength of a binary `strings` sweep. That sweep did
> read the file, but it was filtered on the *email* pattern, and the file
> contains no address — it contains a **Windows user-profile path** (the
> original developer's home directory under a legacy Windows profile
> location), which the filtered sweep did not look for. A later unfiltered
> sweep found it. The lesson matches the repo's own anti-pattern table: a
> guard that checks one *shape* proves only that shape is absent.

---

## 8. Out of scope

- `dev@example.com` / `leftover@example.com` test fixtures (RFC 2606).
- `@andreak3779` in `.github/CODEOWNERS`, `docs/LINKEDIN.md` post-template, and
  `ResumeUrl` — they are the same public handle the repo URL already publishes;
  they are the point of the project, not a leak to fix.
- `AssemblyCompany("Andrea")` in `OriginalSource/AssemblyInfo.cs` (keep-verbatim;
  covered by Decision C).
- The meal catalog, ResumeUrl, Company, CopyrightYear settings.

## 9. Final verification checklist (after Phase 13 + §6)

- [ ] `dotnet build -c Release` → 0 warnings, 0 errors; `dotnet test -c Release` → green, net count 0.
- [ ] `grep -rn ADDRESS` → only `bin/`/`obj/` (regenerable, gitignored) matches.
- [ ] `git grep -n ADDRESS` at the new tip → nothing.
- [ ] `/about` has no mailto link; `ProductInfoOptions` has no `Email` property (guard test pins it).
- [ ] `git log --format='%ae' HEAD~1..HEAD` → noreply identity only for new commits.
- [ ] Tracked docs contain no `/home/<user>` absolute paths.
- [ ] Phase doc `docs/phases/phase-13-remove-email-setting.md` exists; `docs/phases/README.md` row added.
