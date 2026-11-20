# ADR 0011 — Purge personal data from history by publishing a rewritten copy as a new repository

- **Status:** Accepted (Phase 20)
- **Phase:** Phase 20
- **Deciders:** Project owner
- **Supersedes:** [ADR 0008](0008-no-personal-contact-data.md) decision 5 and
  [ADR 0010](0010-redact-pii-in-original-source.md) decision 5 (both
  "no history rewrite"). **Superseded by:** nothing.

## Context

ADRs 0008 and 0010 each chose **not** to rewrite history. The reasoning was
that the owner's address is reconstructible from the public handle, and that
a rewrite changes every SHA and invalidates PR references. ADR 0010 added that
the verified procedure (plan doc §7-B) remained available "as its own approved
task".

The owner has now approved that task. A sweep of every ref, reading **blobs as
bytes in both ASCII and UTF-16LE** (the lesson of ADR 0010), found these still
present in history:

| Shape | Where | Why a working-tree fix did not remove it |
| --- | --- | --- |
| The owner's personal email address | `appsettings.json`; docs; the author, committer and tagger identity on many commits and all 9 tags | Phase 13 removed it from the tree, not from older trees |
| A third party's email address and personal homepage URL | `OriginalSource/frmAbout.cs`; also compiled into legacy `.exe` files as UTF-16 | Phase 19 redacted the tree only |
| The legacy developer's first name and Windows user-profile path | `AssemblyInfo.cs`; legacy IDE state, build output, `.pdb`, upgrade logs | Same |
| The owner's POSIX home directory | `AGENTS.md` and three docs | Phase 13 scrubbed the tree only |

Two facts shaped the mechanism. First, the existing repository already holds
this repository's name, and deleting commits there does not remove GitHub-side
copies of them (pull-request refs, cached commit views). Second, the legacy
binaries embed the strings in UTF-16, which `--replace-text` cannot reach
reliably — they have to be removed, not edited.

## Decision

1. **Publish a rewritten copy as a new repository; do not force-push over the
   old one.** The previous repository was renamed to
   `CookingTime-private-archive` and stays **private**. A new public
   `CookingTime` was created and populated only from the rewritten history.
2. **Rewrite with `git filter-repo`** in a fresh single-branch clone, using
   three mechanisms:
   - **Delete paths** that carry data in forms text replacement cannot reach:
     root `bin/` and `obj/`, `*.suo`, `*.csproj.user`, `UpgradeLog*.XML`.
     None existed at HEAD.
   - **Replace text** in blobs **and** commit messages: the three
     third-party / owner strings, the owner's home-directory prefix (to
     `/home/<user>`), and the personal first name in `AssemblyCompany` (to
     the empty string, matching the Phase 19 working-tree redaction).
   - **Re-stamp identities** with a mailmap: every personal-address identity
     becomes the owner's GitHub noreply address — author, committer and
     tagger.
3. **Publish `main` and the 9 phase tags only.** Dependabot branches, `phase/*`
   branches and local agent checkpoint refs are not published; each is either
   regenerable or already merged into `main`.
4. **Keep the owner's given name and public handle.** ADR 0008 already treats
   them as the project's identity. This ADR removes contact data and
   machine-identifying paths, not authorship.
5. **Accept the deviations from §7-B.** The plan doc's procedure only
   replaced text and rewrote one email. It would have left the legacy binaries
   untouched, and this sweep found the third party's address and the legacy
   developer's name inside them. §7-B is superseded by the mechanism above.

### Verification (recorded so it can be repeated)

Run against a **fresh clone of the published repository**, not the working
copy:

- Byte-level scan of every blob, ASCII and UTF-16LE, for each leaked token:
  no hits (the only residual match is generic prose in the plan doc, not an
  address).
- Every email-shaped string across all history is a GitHub noreply address,
  RFC 2606 `example.com`, or the project's own `.local` fixture.
- Commit messages and tag annotations: no hits.
- `dotnet build -c Release` is warning-free and all 256 tests pass,
  `PersonalDataLeakageTests` included.
- 68 commits, 9 tags. The rewritten tip `be97e88` corresponds to the
  pre-rewrite tip `f695354`.

## Consequences

**Easier**

- A clone of the public repository contains none of the listed data in any
  commit, tag, message or blob.
- The `PersonalDataLeakageTests` guard now describes history as well as the
  tree, so the two no longer disagree.

**Harder**

- **Every SHA changed.** SHAs quoted in older docs (for example in
  `plan-remove-pii.md` and the Phase 13 and 19 docs) refer to the **archive**,
  not to this repository. ADRs and phase docs are append-only, so they are not
  edited; read those SHAs as historical.
- **No PR or issue history carries over.** It lives on in the private archive.
  Dependabot reopens its own pull requests.
- **Repository settings do not transfer:** branch protection, secrets, Pages
  and Actions permissions must be checked on the new repository.
- **The archive still holds all of it**, and is only as private as its access
  list. Do not make it public, and do not push its branches to the new
  repository — that would reintroduce the history this ADR removes.
- **Guarantees stop at what was swept.** The scan checked known tokens in two
  encodings. A shape nobody thought to look for would still pass — the same
  limitation ADR 0010 recorded. It is mitigated, not eliminated, by the
  guard test.

**Neutral**

- Whether copies of the old history exist elsewhere (earlier clones, caches,
  search indexes) from a period when the old repository was reachable cannot
  be determined from inside this repository. The new repository narrows
  future exposure; it does not recall past exposure.
- Plan doc §7-B and its "do it as its own task" framing are now historical.

## References

- [0008-no-personal-contact-data.md](0008-no-personal-contact-data.md) —
  decision 5 superseded.
- [0010-redact-pii-in-original-source.md](0010-redact-pii-in-original-source.md)
  — decision 5 superseded; its byte-level-sweep lesson drove the verification.
- [../plan-remove-pii.md](../plan-remove-pii.md) — §7-B, the original
  procedure.
