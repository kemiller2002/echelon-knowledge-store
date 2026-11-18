# Evidence register

Mission EKS-RESEARCH-2026-003 · 2026-11-18.

**Types:**
- **M**: measured by this mission (command or run reproducible from the listed files).
- **O**: observed by reading code or docs (path given).
- **C**: cited from earlier Echelon research.
- **V**: vendor claim.

Paths are relative to this directory unless they name a repository.

| ID | Type | Statement | Source / reproduction |
|---|---|---|---|
| EV-AE-001 | M | Dokimos `Coverage.admit` kept the last outcome per identity; 3 of 4 adversarial cases verified over a recorded failure on main 6ea8624 | `verification/dokimos-worst-outcome-before.txt`; dokimos#71 `tests/Dokimos.Core.Tests/AdversarialVerificationTests.fs` |
| EV-AE-002 | M | With worst-outcome merging, the Dokimos suite passes 602/602 with 0 warnings, and the self-ratchet passes | `verification/dokimos-worst-outcome-after.txt`; `dotnet test Dokimos.sln -c Release` |
| EV-AE-003 | M | Praxis main 12bf995 accepted `/etc/hostname`, `../outside.txt` and an absolute temp path as present completion evidence | `verification/praxis-evidence-escape-before.txt`, `praxis-evidence-escape-repro.fsx` |
| EV-AE-004 | M | About 2,470 historical tests/implementation citations across 8 repos: 1 outside its repository (a `/tmp` agent scratchpad in Dokimos), 30 directories, 415 (Praxis) and 7 (other repos) now missing | `verification/evidence-path-scan.txt` (`scripts/evidence-path-scan.sh`) |
| EV-AE-005 | M | 0 of 13 `ros.json` files enable `requireVerifiedCompletion`, `requireAttestedEvidence`, `trustedVerifierKeyIds` or `qualityEvidence` | `verification/completion-policy-scan.txt` (`scripts/completion-policy-scan.sh`) |
| EV-AE-006 | O | The `behavior-verified` facet is satisfied by presence of `tests=` evidence; documented as "an attestation, not a proof" | `praxis/src/Praxis.Domain/Work/CompletionReadiness.fs` (`presenceFacet`); `praxis/docs/work-protocol.md:200` |
| EV-AE-007 | O | `complete-verified` checks claim, revision, status, lease and recording; protected policy from the merge base; pinned verifier keys | `praxis/src/Praxis.Domain/Work/ValidationResult.fs`, `CompletionGate.fs`, `Application/Work/ProtectedCompletionPolicy.fs`, `docs/verified-completion-rollout.md` |
| EV-AE-008 | M | Praxis full suite on the confinement branch after the golden update: no failure that does not also occur on untouched main | `verification/praxis-full-suite-after.txt`, `verification/praxis-preexisting-failures-on-main.txt` |
| EV-AE-009 | O | Golden case `test6` froze "absolute paths outside the repository are accepted" | `praxis/tests/Praxis.Tests/Golden/work.json`, `WorkDecisionCliTests.fs` |
| EV-AE-010 | M | Governed-proposal contract: 12/12 tests; architecture 11/11, naming 30/30 and internal 12/12 guards pass | praxis#302; `PRAXIS_TEST_FILTER="governed learning" dotnet run --project tests/Praxis.Tests -c Release` |
| EV-AE-011 | M | EXP-AE-001 round 1 (Opus-class): 36/36 verified; analysis arms 1.29–2.24× baseline wall time | `experiments/clarity-edf/raw/round1/`, `results-summary.json` |
| EV-AE-012 | M | EXP-AE-001 round 2 (Haiku): A 5/6, B 6/6, C 6/6, D 4/6, E 6/6, N 5/6; "complete" with oracle failure only in A-T4 (undisclosed) and D-T4 (residual disclosed, requester asked) | `experiments/clarity-edf/raw/round2/`, `results-summary.json`; every run's final report in `raw/round*/runs/<cell>/FINAL_MESSAGE.md` |
| EV-AE-013 | M | The subagent runtime refused `ANALYSIS.md` writes in many runs; 2 runs stopped as a result | `raw/round1/deviations.md`, `raw/round2/deviations.md` |
| EV-AE-014 | M | Haiku subagents made unrequested remote-session tool calls; no child session was created and no other session was updated | `raw/round2/deviations.md`; `list_sessions` check recorded in the final report |
| EV-AE-015 | V / M | AutoBuild capabilities and quantitative claims; about 5,706 public bot PRs counted by this mission | `data/autobuild-research-notes.md`, `data/autobuild-capabilities.json` |
| EV-AE-016 | C | Prior Clarity and EDF studies: no accuracy gain on decision case files; higher-effort reasoning beat EDF | `data/clarity-edf-audit.md` §3 (from clarity-framework and EDF research trees) |
| EV-AE-017 | C | EKS mission 002: compiled packages 32/32 vs 24/32 on the hard fixture; 0 vs 8 false completion claims. Mission 001: syntax ≤13% token effect | `research/2026-compiler-integration/continuation/research-status.json`; `research/2026-knowledge-representation/strategy/` |
| EV-AE-018 | M / O | Baseline inventory of 14 systems: versions, builds and tests, gaps, reuse candidates | `data/baseline-group-a.json`, `data/baseline-group-b.json` |
| EV-AE-019 | M | Before the golden update, 2,076 passed and 21 failed; 20 of the failures reproduce on untouched main (environmental subprocess tests) | `verification/praxis-full-suite-before-golden-update.txt`, `praxis-preexisting-failures-on-main.txt` |
| EV-AE-020 | O | Receipt verification and obligation types duplicated in Praxis and Dokimos; Praxis pins Ordo.Core 1.8.2 against Ordo 1.20.0 | `data/baseline-group-a.json` |
| EV-AE-021 | O | Fides' own gap report lists high-severity token-scope issues | `data/baseline-group-a.json` (Fides entry) |
| EV-AE-022 | O | "Aegis" handles operational faults and recovery, not security; Vigila is a follow-up organiser, not monitoring | `data/baseline-group-a.json` |
| EV-AE-023 | O | `praxis web serve` and `hub serve` already provide queue, work, validation and execution pages on pinned Forma and Limen routing, separating human-required actions | `praxis/src/Praxis.Cli/WebInterface.fs`, `WebExecutions.fs`, `praxis/docs/application-foundations.md` |
| EV-AE-024 | M | Every visible-test modification in 72 runs added tests; none removed or weakened an original test or assertion | `scripts/test-weakening-check.py`; `raw/round*/graded-*.json` `visibleTestsTouched` |
| EV-AE-025 | M | Independent adversarial review: reproduced all 72 grades; found a parent-symlink bypass of the Praxis confinement fix, six ways to game the learning contract, an over-read of D-T4, and assumption-driven economics. All corrected or documented | `adversarial-review/review.md` and reproductions |
| EV-AE-026 | M | The pre-registration files were written before any run output (round 1 15:26:42Z vs first report 15:28:01Z; round 2 15:30:07Z vs 15:30:56Z). The originally recorded `registeredAt` values were hand-entered and wrong | `raw/round*/preregistration.json` (`registeredAtCorrection`) |
