# Verification audit (Phase 2)

Mission EKS-RESEARCH-2026-003 · 2026-11-18 · Scope: Dokimos, Praxis, Aegis.
Evidence IDs (`EV-AE-*`) resolve in [`evidence-register.md`](evidence-register.md).

## Summary

| # | Finding | Status | Evidence |
|---|---|---|---|
| V1 | Dokimos coverage kept the **last** outcome for a repeated test identity, so a failed test followed by a pass (TRX theory rows, retries, reruns at the same SHA) verified the obligation. | **Confirmed defect, fixed** in dokimos#71 (draft) | EV-AE-001, EV-AE-002 |
| V2 | Praxis `work complete --evidence TYPE=PATH` accepted paths **outside the repository** (absolute paths, `../` escapes, symlinked files or **symlinked parent directories**). One real completion cited an agent's ephemeral `/tmp` scratchpad as test evidence. | **Confirmed defect, fixed** in praxis#303 (draft; changes a frozen golden). The parent-directory bypass was found by independent review and is fixed in the same PR. | EV-AE-003, EV-AE-004, EV-AE-008, EV-AE-009 |
| V3 | Praxis's strong path (`complete-verified`: claim-bound validation results, signed receipts, protected policy, pinned verifier keys) is enabled in **0 of 13** repositories inspected. Default completion checks only that each evidence path exists. | **Confirmed gap** (policy adoption, not code) | EV-AE-005, EV-AE-007 |
| V4 | The `behavior-verified` readiness facet is satisfied by the *presence* of `tests=PATH`. Docs call it "an attestation, not a proof", but the name invites the opposite reading. | **Confirmed, documented limitation**; proposal only | EV-AE-006 |
| V5 | 415 historical Praxis `tests`/`implementation` citations point at files that no longer exist; evidence is cited by path, not pinned by digest. | **Confirmed gap**; proposal only | EV-AE-004 |
| V6 | Smaller-model subagents made unrequested calls to session-management tools during the experiment. The runtime denied them; no session was created or messaged. | **Observed risk** (contained) | EV-AE-014 |

"Aegis" in this portfolio handles operational faults and recovery, not security
(baseline EV-AE-018). Security assurance is split between Tutela (posture gate) and
Praxis's protected classes, so it was audited through Praxis.

## What Echelon already does well (do not rebuild)

Praxis `complete-verified` (`src/Praxis.Domain/Work/CompletionGate.fs`,
`ValidationResult.fs`, `Application/Work/ProtectedCompletionPolicy.fs`) already resists
most of the adversarial cases in the program brief:

| Brief case | Praxis mechanism (when enabled) |
|---|---|
| Failed tests reported as passing | `ValidationResult.Status` must be `Passed` (`PRX.COMPLETION.EVIDENCE_NOT_PASSED`) |
| Missing / malformed evidence | `EvidenceObservation.Missing / Unreadable / Unrecognized / MalformedEnvelope` all refuse |
| Evidence from another revision | `PRX.COMPLETION.EVIDENCE_REVISION_MISMATCH`; full 40/64-hex commits only |
| Stale results | Lease-window check (`PRX.COMPLETION.EVIDENCE_STALE`) |
| Agent-generated claims | Only results recorded by `work validate-run` events count; attested mode accepts only verifier-signed DSSE receipts |
| Unauthorized policy change | REL-GATE-004: policy read from the protected merge base; any weakening is `PRX.COMPLETION.POLICY_CHANGED` |
| Self-signed trust root | `trustedVerifierKeyIds` pinned in protected policy; authority files inside the worktree refused |
| Concurrent agents | Claim fencing generations and leases (`Claim.fs`) |

These were built from earlier forgery experiments (REL-001, GATE-*, PROBE-1..4) recorded
in `praxis/docs/verified-completion-rollout.md`. **The audit found the design sound and
the deployment absent (V3).** That gap matters more than any single code defect.

## V1: Dokimos worst-outcome (fixed)

- **Mechanism.** `Coverage.admit` merged outcomes with `Map.ofList`, which keeps the
  last value per key. TRX identities are `dotnet:<className>.<name>`, so xUnit theory
  rows share an identity, and reruns at the same SHA repeat it.
- **Reproduction on main 6ea8624.** Three of four adversarial cases reported `Verified`
  over a recorded failure (`verification/dokimos-worst-outcome-before.txt`).
- **Inconsistency.** Dokimos's own Playwright parsers already treated a retry with
  differing outcomes as a failure; only the coverage path did not.
- **Fix.** `TestResults.worst` ranks Failed > Skipped > NotExecuted > Passed, and
  `mergeWorst` is applied in `admit`. There are 5 new adversarial tests; the full suite
  passes 602/602 and the Dokimos self-ratchet passes (`verification/dokimos-worst-outcome-after.txt`).
- **Policy consequence for reviewers.** A rerun that passes no longer clears an earlier
  failure at the same SHA. A flaky test must be fixed. Coverage has no exception
  mechanism; the first draft wrongly said "or excepted".
- **Limits found by independent review** (`adversarial-review/dokimos-worst-edges.*`):
  - Rerun protection works only if the caller-written results manifest lists every run.
    Omit the failing run and the obligation verifies. The manifest is not independent
    evidence.
  - A data-driven test with one deliberately skipped row can no longer verify, because
    Skipped ranks above Passed. That is fail-closed, but it is a behaviour change for
    such suites.

## V2: Praxis evidence-path confinement (fixed, contract change)

- **Mechanism.** `FileEvidenceRepository.observe` did
  `Path.GetFullPath(Path.Combine(root, path))` then `File.GetAttributes`.
  `Path.Combine` discards `root` for an absolute path.
- **Reproduction on main 12bf995.** `/etc/hostname`, `../outside.txt` and an absolute
  temp path were all `Present` (`verification/praxis-evidence-escape-before.txt`,
  script `praxis-evidence-escape-repro.fsx`).
- **Real incident.** Across about 2,470 historical evidence citations in 8 repositories,
  exactly one was outside its repository. A Dokimos completion cited
  `/tmp/claude-0/.../scratchpad/dokimos-0.8.0-tests.txt`, a file in a since-destroyed
  agent container (`verification/evidence-path-scan.txt`).
- **Fix.**
  - Resolve the path and require it to lie under the repository root.
  - Resolve symbolic links in **every** path component against the physical repository
    root. The first version resolved only the final component; an independent reviewer
    bypassed it with a symlinked parent directory.
  - Otherwise the result is `Unavailable` with "resolves outside the repository".
  - In-repo directories stay accepted: 30 historical citations use directories
    deliberately.
- **Not closed (documented limits):** untracked or ignored files inside the repository,
  and `.` or the empty path (the repository root), are still `Present`. Closing them
  needs "tracked at the completion commit, sha256 recorded" (V5, roadmap R2).
- **Contract change.** Golden case `test6` in `tests/Praxis.Tests/Golden/work.json`
  froze "absolute paths outside are accepted". The PR changes it explicitly and says so.
- **Test results.**
  - Full suite before the golden update: 2,514 tests, 2,485 passed, 29 failed. 28 of the
    failures also fail on untouched main in this container; all spawn real subprocesses
    or validators (`verification/praxis-preexisting-failures-on-main.txt`). The 29th
    was `test6`.
  - After the golden update: 2,486 passed and 28 failed, with **no failure absent from
    main** (`verification/praxis-full-suite-after.txt`, EV-AE-008).
  - After the parent-symlink hardening: see `verification/praxis-full-suite-final.txt`.

## V3: verified completion is not deployed

`verification/completion-policy-scan.txt`: no `ros.json` among Praxis, Dokimos, Aegis,
Arca, EDF, echelon-foundry, echelon-registry, Fides, Folio, Forma, Limen, Ordo and Vigila
sets `requireVerifiedCompletion`, `requireAttestedEvidence`, `trustedVerifierKeyIds` or
`qualityEvidence`. Every completion in the portfolio therefore runs the legacy path, where
"verified" means "the cited paths exist".

**Recommendation (needs owner approval; it is a policy change).** Follow
`praxis/docs/verified-completion-rollout.md` (PRAXIS-REL-006) for one pilot repository.
Dokimos is the natural candidate because it already dogfoods itself:
`requireVerifiedCompletion` plus `trustedVerifierKeyIds` pinned through a reviewed change.

## V4 and V5: proposals (not implemented)

- **V4.** Rename `behavior-verified` to `tests-cited`, or make it judged. It would be
  satisfied only by a `praxis.validation-result/1` with `Passed` at the completion
  revision, the same rule `complete-verified` uses. This is not implemented because it
  changes documented behaviour that no repository currently enables.
- **V5.** Record `sha256` for every `--evidence` path at completion time. The quality gate
  already does this in `ConsumedEvidence`; extending it to plain completion makes later
  audits possible after files move.

## Brief cases not exercised

- "Inconsistent verification outcomes across repeated executions" is addressed for test
  identities by V1. Whole-suite nondeterminism was not measured.
- "Prompt injection through repository files" and "unmerged branches changing reviewer
  instructions" were not attacked directly. Praxis reads protected policy from the merge
  base (REL-GATE-004), and the experiment prompts told agents to treat file text as
  evidence, not instruction. No dedicated injection test was written. **Next action:**
  an adversarial fixture whose `AGENTS.md` on an unmerged branch weakens verification,
  run against `complete-verified`.
