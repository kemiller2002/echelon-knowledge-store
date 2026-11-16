# Privacy review

## Incident (Phase 1)

Phase 1 audited every repository in the workspace, including two private ones. Audit reports
committed to this public repository named private directories and files: six lines, in three files,
in commit `bc4bc05`. Commit `3aeb52b` replaced them with placeholders. History was not rewritten
(Phase 1's rule was "never force-push"), so the original lines remain readable in `bc4bc05`.

## Review findings (this phase)

1. **The Phase 2 CI check would not have caught the incident.** The denylist added at the start of
   this phase (`.privacy-denylist`, CI job `privacy`) matches `private-repo/path`. Tested against the six
   historical lines: **0 of 6 matched**. The leaked paths appeared without the repository prefix, in
   table cells, JSON fields and backticks. A check that passes on the incident it was written for
   gives false assurance.
2. **Three further leaks were still in the current tree.** A stronger local check (below) found three
   private file names in Phase 1 documents (`baseline/cross-repository-dependency-inventory.md`,
   two references; `baseline/current-agent-workflows.md`, one reference) that no public repository
   mentions. They were redacted in this phase. Like the original incident, they remain in history:
   commits from `bc4bc05` up to the commit that redacts them.
3. **No Phase 2 artefact copies private content.** All fixtures are synthetic. Phase 2 experiments
   read only public repositories and the synthetic fixtures. Container paths of the research sandbox
   (`/home/user/…`) appear in Phase 1 documents. They are judged not sensitive (sandbox layout, no user
   data) and are not in the denylist.

## Controls now in place

| Control | Where | Catches | Limits |
|---|---|---|---|
| `.privacy-denylist` + `eks privacy-check` | CI job `privacy` on every push | `private-repo/path` forms; scratch paths | Misses unprefixed paths (0 of 6 historical lines). Runs where private trees are absent, so it cannot know private names. |
| `eks privacy-check-private WORKSPACE --private a,b` | local, before committing research that read private repositories | distinctive directory and file names (≥ 10 characters, containing `-`, `.`, `_` or a capital letter) from the private trees, excluding names that any public repository already mentions in its text | Needs the private trees on disk; cannot run in CI. Misses short or generic names and paraphrase. Never prints a matched name, only a hash. |

Validation of the local check: on the six historical lines it reports 4. The other two contain only
names that public repositories already mention, so they are not private. On the current tree it
reported the three findings above. After redaction it reports none (398 distinctive names checked).

## Remediation options for history (owner decision)

The agent must not rewrite history or force-push. Options for the repository owner:

1. **Rewrite history** (for example `git filter-repo` replacing the affected lines in `bc4bc05` and
   later commits), then force-push `main`. This invalidates existing clones, PR references and
   commit hashes cited in research documents (including this one), and it does not remove copies
   already fetched or cached by others.
2. **Make the repository private** while deciding. That stops new exposure and keeps history.
3. **Accept and document.** The leaked items are file and directory names, not file contents. The
   owner judges their sensitivity.

Recommendation: the owner reviews the nine affected lines (six historical, three recent) and decides.
If any name is sensitive, choose 2, then 1. Until then, no further mitigation is possible from this
session.

## Rule for future research

Before committing anything produced after reading private repositories, run both checks:
`eks privacy-check --root .` and
`eks privacy-check-private <workspace> --root . --private <private repos>`. Phase 2 commits after
the check was built were made with both passing.
